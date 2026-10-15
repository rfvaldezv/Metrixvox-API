using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MetrixVox.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace MetrixVox.Application.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateAccessToken(User user, out DateTime expiresAt)
        {
            var jwtSection = _config.GetSection("Jwt");
            var secretKey = jwtSection["SecretKey"]
                ?? throw new InvalidOperationException("Falta Jwt:SecretKey en configuración.");
            var issuer = jwtSection["Issuer"] ?? "api.metrixvox.com";
            var audience = jwtSection["Audience"] ?? "metrixvox";
            var expirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var m) ? m : 60;

            expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("role", user.Role.ToString()),
                new("tenantId", user.TenantId),
                new("name", user.FullName),
            };

            // Agrega el rol estándar para [Authorize(Roles = "...")]
            claims.Add(new Claim(ClaimTypes.Role, user.Role.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
