using System.Security.Cryptography;
using MetrixVox.Application.DTOs;
using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
                .FirstOrDefaultAsync(k => k.KioskToken == kioskToken && k.IsActive);

            if (kiosk == null) return null;

            // Actualizar LastSeenAt en background
            _ = Task.Run(async () =>
            {
                try
                {
                    await TouchAsync(kiosk.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo actualizar LastSeenAt del kiosco {KioskId}", kiosk.Id);
                }
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
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId,
            };

            // Si viene override (SuperAdmin), asignar el tenant explícitamente.
            // StampTenant respetará este valor porque ya no está vacío.
            if (!string.IsNullOrWhiteSpace(tenantOverride))
            {
                kiosk.TenantId = tenantOverride.Trim().ToUpperInvariant();
            }

            _context.Kiosks.Add(kiosk);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Kiosco creado: {Name} ({Tenant}) → token {Token}",
                kiosk.Name, kiosk.TenantId, kiosk.KioskToken);

            return new CreateKioskResponse
            {
                Kiosk = MapToDto(kiosk),
                KioskToken = kiosk.KioskToken,
                Message = $"Kiosco '{kiosk.Name}' creado en tenant {kiosk.TenantId}. " +
                          $"Configura este token en la terminal: {kiosk.KioskToken}"
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

        private static KioskDto MapToDto(Kiosk k) => new()
        {
            Id = k.Id,
            TenantId = k.TenantId,
            Name = k.Name,
            BranchName = k.BranchName,
            SurveyCode = k.SurveyCode,
            KioskToken = k.KioskToken,
            IsActive = k.IsActive,
            CreatedAt = k.CreatedAt,
            LastSeenAt = k.LastSeenAt,
        };

        private static string GenerateKioskToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
