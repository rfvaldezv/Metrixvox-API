using System.Security.Cryptography;
using MetrixVox.Application.DTOs;
using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MetrixVox.Application.Services
{
    public class KioskService : IKioskService
    {
        private readonly TenantDbContext _context;
        private readonly ILogger<KioskService> _logger;

        public KioskService(TenantDbContext context, ILogger<KioskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<KioskConfigDto?> GetByTokenAsync(string kioskToken)
        {
            if (string.IsNullOrWhiteSpace(kioskToken)) return null;

            var kiosk = await _context.Kiosks
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.KioskToken == kioskToken && k.IsActive && k.IsPaired);

            if (kiosk == null) return null;

            // Actualizar LastSeenAt en background
            _ = Task.Run(async () =>
            {
                try { await TouchAsync(kiosk.Id); }
                catch (Exception ex) { _logger.LogWarning(ex, "No se pudo actualizar LastSeenAt del kiosco {KioskId}", kiosk.Id); }
            });

            return new KioskConfigDto
            {
                TenantId = kiosk.TenantId,
                SurveyCode = kiosk.SurveyCode,
                BranchName = kiosk.BranchName,
                KioskName = kiosk.Name,
            };
        }

        public async Task<List<KioskDto>> ListAsync()
        {
            return await _context.Kiosks
                .AsNoTracking()
                .OrderBy(k => k.Name)
                .Select(k => MapToDto(k))
                .ToListAsync();
        }

        public async Task<CreateKioskResponse> CreateAsync(CreateKioskRequest request, Guid? createdByUserId, string? tenantOverride = null)
        {
            var kiosk = new Kiosk
            {
                Id = Guid.NewGuid(),
                KioskToken = GenerateKioskToken(),
                Name = request.Name.Trim(),
                BranchName = request.BranchName.Trim(),
                SurveyCode = request.SurveyCode.Trim(),
                IsActive = true,
                IsPaired = false,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId,
            };

            if (!string.IsNullOrWhiteSpace(tenantOverride))
            {
                kiosk.TenantId = tenantOverride.Trim().ToUpperInvariant();
            }

            _context.Kiosks.Add(kiosk);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Kiosco creado: {Name} ({Tenant})", kiosk.Name, kiosk.TenantId);

            return new CreateKioskResponse
            {
                Kiosk = MapToDto(kiosk),
                Message = $"Kiosco '{kiosk.Name}' creado. Genera un código de vinculación desde el panel para emparejar la tablet."
            };
        }

        public async Task<KioskDto?> UpdateAsync(Guid id, UpdateKioskRequest request)
        {
            var kiosk = await _context.Kiosks.FirstOrDefaultAsync(k => k.Id == id);
            if (kiosk == null) return null;

            kiosk.Name = request.Name.Trim();
            kiosk.BranchName = request.BranchName.Trim();
            kiosk.SurveyCode = request.SurveyCode.Trim();
            kiosk.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return MapToDto(kiosk);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var kiosk = await _context.Kiosks.FirstOrDefaultAsync(k => k.Id == id);
            if (kiosk == null) return false;

            kiosk.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task TouchAsync(Guid kioskId)
        {
            var kiosk = await _context.Kiosks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.Id == kioskId);

            if (kiosk != null)
            {
                kiosk.LastSeenAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // ==========================================
        // LÓGICA DE PAIRING (NUEVA)
        // ==========================================

        public async Task<string> GeneratePairingCodeAsync(Guid kioskId)
        {
            var kiosk = await _context.Kiosks.IgnoreQueryFilters().FirstOrDefaultAsync(k => k.Id == kioskId);
            if (kiosk == null) throw new KeyNotFoundException("Kiosco no encontrado");

            // Generar código de 6 caracteres alfanuméricos
            var code = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            
            kiosk.PairingCode = code;
            kiosk.PairingCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
            kiosk.IsPaired = false; 
            
            // Rotamos el token por seguridad
            kiosk.KioskToken = GenerateKioskToken(); 

            await _context.SaveChangesAsync();
            _logger.LogInformation("Código de vinculación generado para kiosco {KioskId}: {Code}", kioskId, code);
            
            return code;
        }

        public async Task<KioskConfigDto?> PairDeviceAsync(string pairingCode)
        {
            if (string.IsNullOrWhiteSpace(pairingCode)) return null;

            var kiosk = await _context.Kiosks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(k => k.PairingCode == pairingCode.Trim().ToUpperInvariant());

            if (kiosk == null || kiosk.PairingCodeExpiresAt < DateTime.UtcNow || !kiosk.IsActive)
                return null;

            kiosk.IsPaired = true;
            kiosk.PairingCode = null;
            kiosk.PairingCodeExpiresAt = null;
            kiosk.LastSeenAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Tablet vinculada exitosamente al kiosco {KioskId}", kiosk.Id);

            return new KioskConfigDto
            {
                KioskToken = kiosk.KioskToken,
                TenantId = kiosk.TenantId,
                SurveyCode = kiosk.SurveyCode,
                BranchName = kiosk.BranchName,
                KioskName = kiosk.Name,
            };
        }

        public async Task<bool> RevokePairingAsync(Guid kioskId)
        {
            var kiosk = await _context.Kiosks.IgnoreQueryFilters().FirstOrDefaultAsync(k => k.Id == kioskId);
            if (kiosk == null) return false;

            kiosk.IsPaired = false;
            kiosk.KioskToken = GenerateKioskToken(); // Invalida el token de la tablet vieja
            
            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================

        private static KioskDto MapToDto(Kiosk k) => new()
        {
            Id = k.Id,
            TenantId = k.TenantId,
            Name = k.Name,
            BranchName = k.BranchName,
            SurveyCode = k.SurveyCode,
            IsActive = k.IsActive,
            IsPaired = k.IsPaired,
            CreatedAt = k.CreatedAt,
            LastSeenAt = k.LastSeenAt,
        };

        private static string GenerateKioskToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
