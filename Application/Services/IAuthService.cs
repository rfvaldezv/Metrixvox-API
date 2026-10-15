using MetrixVox.Application.DTOs;

namespace MetrixVox.Application.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<LoginResponseDto?> RefreshAsync(string refreshToken);
        Task<bool> LogoutAsync(Guid userId);
        Task<UserDto?> GetCurrentUserAsync(Guid userId);
    }
}
