using System.Security.Claims;
using BCrypt.Net;
using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/users")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly TenantDbContext _context;

        public AdminUsersController(TenantDbContext context)
        {
            _context = context;
        }

        /// <summary>Lista los usuarios del tenant activo.</summary>
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var users = await _context.Users
                .IgnoreQueryFilters()
                .Where(u => u.TenantId == GetActiveTenant())
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    Role = u.Role.ToString(),
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt,
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>Crea un usuario nuevo en el tenant activo.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { error = "Email, password y fullName son obligatorios." });
            }

            if (request.Password.Length < 6)
                return BadRequest(new { error = "La contraseña debe tener al menos 6 caracteres." });

            var email = request.Email.Trim().ToLowerInvariant();

            var exists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == email);
            if (exists)
                return Conflict(new { error = "Ya existe un usuario con ese email." });

            if (!Enum.TryParse<UserRole>(request.Role, out var role))
                role = UserRole.Viewer;

            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = GetActiveTenant(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FullName,
                Role = user.Role.ToString(),
                user.IsActive,
            });
        }

        /// <summary>Actualiza un usuario (nombre, rol, activo).</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            user.FullName = request.FullName.Trim();
            if (Enum.TryParse<UserRole>(request.Role, out var role))
                user.Role = role;
            user.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Usuario actualizado." });
        }

        /// <summary>Resetea la contraseña de un usuario.</summary>
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return BadRequest(new { error = "La nueva contraseña debe tener al menos 6 caracteres." });

            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada." });
        }

        private string GetActiveTenant()
        {
            // El tenant viene del header X-Tenant-Id (ya validado por el middleware)
            var header = Request.Headers["X-Tenant-Id"].ToString().Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(header)) return header;

            // Fallback al claim del JWT
            var jwtTenant = User.FindFirst("tenantId")?.Value;
            return jwtTenant ?? string.Empty;
        }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Viewer";
    }

    public class UpdateUserRequest
    {
        public string FullName { get; set; } = "";
        public string Role { get; set; } = "Viewer";
        public bool IsActive { get; set; } = true;
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = "";
    }
}
