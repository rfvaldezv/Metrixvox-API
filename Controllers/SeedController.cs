using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Api.Controllers
{
    /// <summary>
    /// Endpoints TEMPORALES para inicializar la plataforma.
    /// ⚠️ Se deben eliminar después del primer setup en producción.
    /// Solo funcionan si NO existe ningún usuario aún.
    /// </summary>
    [ApiController]
    [Route("api/v1/setup")]
    public class SeedController : ControllerBase
    {
        private readonly TenantDbContext _context;
        private readonly ILogger<SeedController> _logger;

        public SeedController(TenantDbContext context, ILogger<SeedController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea el primer SuperAdmin. Solo funciona si la tabla Users está vacía.
        /// POST /api/v1/setup/first-admin
        /// Body: { "email": "...", "password": "...", "fullName": "..." }
        /// </summary>
        [HttpPost("first-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateFirstAdmin([FromBody] FirstAdminRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { error = "Email, password y fullName son obligatorios." });
            }

            if (request.Password.Length < 6)
                return BadRequest(new { error = "La contraseña debe tener al menos 6 caracteres." });

            // Bloqueo: solo funciona si la tabla está vacía
            var alreadyHasUsers = await _context.Users.IgnoreQueryFilters().AnyAsync();
            if (alreadyHasUsers)
            {
                _logger.LogWarning("Intento de crear first-admin cuando ya existen usuarios.");
                return Conflict(new
                {
                    error = "Ya existen usuarios en la plataforma. Este endpoint está deshabilitado."
                });
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = "__SUPER__",  // valor especial para SuperAdmin (sin tenant real)
                Email = request.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("SuperAdmin creado: {Email}", user.Email);

            return Ok(new
            {
                message = "SuperAdmin creado correctamente. Ya puedes hacer login en /api/v1/auth/login",
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FullName,
                    Role = user.Role.ToString(),
                }
            });
        }
    }

    public class FirstAdminRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
