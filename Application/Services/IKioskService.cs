using MetrixVox.Application.DTOs;

namespace MetrixVox.Application.Services
{
    public interface IKioskService
    {
        Task<KioskConfigDto?> GetByTokenAsync(string kioskToken);
        Task<List<KioskDto>> ListAsync();
        Task<CreateKioskResponse> CreateAsync(CreateKioskRequest request, Guid? createdByUserId, string? tenantOverride = null);
        Task<KioskDto?> UpdateAsync(Guid id, UpdateKioskRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task TouchAsync(Guid kioskId);

        // --- Nuevos métodos para el Pairing Flow ---
        Task<string> GeneratePairingCodeAsync(Guid kioskId);
        Task<KioskConfigDto?> PairDeviceAsync(string pairingCode);
        Task<bool> RevokePairingAsync(Guid kioskId);
    }
}
