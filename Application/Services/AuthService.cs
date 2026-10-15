using MetrixVox.Application.DTOs;
using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly TenantDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            TenantDbContext context,
            IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            // La tabla Users es global. IgnoreQueryFilters evita el filtro por tenant.
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Login fallido: email no encontrado {Email}", request.Email);
                return null;
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login fallido: password incorrecto {Email}", request.Email);
                return null;
            }

            var accessToken = _jwtService.GenerateAccessToken(user, out var expiresAt);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Login exitoso {Email} ({Role})", user.Email, user.Role);

            return new LoginResponseDto
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = MapToDto(user),
            };
        }

        public async Task<LoginResponseDto?> RefreshAsync(string refreshToken)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.IsActive);

            if (user == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
                return null;

            var accessToken = _jwtService.GenerateAccessToken(user, out var expiresAt);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {
                Token = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = expiresAt,
                User = MapToDto(user),
            };
        }

        public async Task<bool> LogoutAsync(Guid userId)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
        {
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user == null ? null : MapToDto(user);
        }

        private static UserDto MapToDto(User user) => new()
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            TenantId = user.TenantId,
            Role = user.Role.ToString(),
        };
    }
}
