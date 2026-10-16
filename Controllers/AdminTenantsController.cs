using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/tenants")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminTenantsController : ControllerBase
    {
        private readonly TenantDbContext _context;
        private readonly ILogger<AdminTenantsController> _logger;

        public AdminTenantsController(TenantDbContext context, ILogger<AdminTenantsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>Lista todos los tenants.</summary>
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var tenants = await _context.Tenants
                .IgnoreQueryFilters()
                .OrderBy(t => t.DisplayName)
                .Select(t => new
                {
                    id = t.Id,
                    tenantId = t.TenantId,
                    name = t.DisplayName,
                    displayName = t.DisplayName,
                    contactEmail = t.ContactEmail,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
                })
                .ToListAsync();

            return Ok(tenants);
        }

        /// <summary>Obtiene un tenant por Id.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id);

            return tenant == null ? NotFound() : Ok(tenant);
        }

        /// <summary>Crea un tenant nuevo.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TenantId) ||
                string.IsNullOrWhiteSpace(request.DisplayName))
                return BadRequest(new { error = "TenantId y DisplayName son obligatorios." });

            var tenantId = request.TenantId.Trim().ToUpperInvariant();

            if (!System.Text.RegularExpressions.Regex.IsMatch(tenantId, "^[A-Z0-9_]{3,64}$"))
                return BadRequest(new { error = "El TenantId solo admite letras, números y guion bajo (3-64)." });

            var exists = await _context.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.TenantId == tenantId);
            if (exists)
                return Conflict(new { error = "Ya existe un tenant con ese código." });

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                DisplayName = request.DisplayName.Trim(),
                ContactEmail = request.ContactEmail?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tenant creado: {TenantId}", tenant.TenantId);

            return Ok(new
            {
                id = tenant.Id,
                tenantId = tenant.TenantId,
                displayName = tenant.DisplayName,
                isActive = tenant.IsActive,
            });
        }

        /// <summary>Actualiza un tenant.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request)
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            tenant.DisplayName = request.DisplayName.Trim();
            tenant.ContactEmail = request.ContactEmail?.Trim();
            tenant.IsActive = request.IsActive;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Tenant actualizado." });
        }

        /// <summary>Soft delete de un tenant.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            tenant.IsActive = false;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Tenant desactivado." });
        }
    }

    public class CreateTenantRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
    }

    public class UpdateTenantRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
