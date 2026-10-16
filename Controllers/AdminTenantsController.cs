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

        public AdminTenantsController(TenantDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista los tenants únicos que tienen datos en el sistema.
        /// Solo SuperAdmin. Ignora el filtro global porque necesita ver todos.
        /// GET /api/v1/admin/tenants
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List()
        {
            // Obtener tenants únicos de las tablas principales
            var fromSurveys = await _context.SurveyDefinitions
                .IgnoreQueryFilters()
                .Select(s => s.TenantId)
                .Distinct()
                .ToListAsync();

            var fromKiosks = await _context.Kiosks
                .IgnoreQueryFilters()
                .Select(k => k.TenantId)
                .Distinct()
                .ToListAsync();

            var fromResponses = await _context.SurveyResponseRecords
                .IgnoreQueryFilters()
                .Select(r => r.TenantId)
                .Distinct()
                .ToListAsync();

            var fromUsers = await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId != "__SUPER__")
                .Select(u => u.TenantId)
                .Distinct()
                .ToListAsync();

            // Unir todos, descartar el especial __SUPER__ y ordenar
            var allTenants = fromSurveys
                .Concat(fromKiosks)
                .Concat(fromResponses)
                .Concat(fromUsers)
                .Where(t => !string.IsNullOrWhiteSpace(t) && t != "__SUPER__")
                .Distinct()
                .OrderBy(t => t)
                .Select(t => new { tenantId = t, name = t })
                .ToList();

            return Ok(allTenants);
        }
    }
}
