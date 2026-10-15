using MetrixVox.Domain.Entities;

namespace MetrixVox.Application.Services
{
    public interface IJwtService
    {
        /// <summary>Genera un JWT de acceso para el usuario.</summary>
        string GenerateAccessToken(User user, out DateTime expiresAt);

        /// <summary>Genera un refresh token aleatorio.</summary>
        string GenerateRefreshToken();
    }
}
