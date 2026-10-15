using MetrixVox.Application.DTOs;

namespace MetrixVox.Application.Services
{
    public interface IKioskService
    {
        /// <summary>Obtiene la config del kiosco por su token (público, sin JWT).</summary>
        Task<KioskConfigDto?> GetByTokenAsync(string kioskToken);

        /// <summary>Lista los kioscos del tenant actual.</summary>
        Task<List<KioskDto>> ListAsync();

        /// <summary>Crea un kiosco nuevo con token generado.</summary>
        Task<CreateKioskResponse> CreateAsync(CreateKioskRequest request, Guid? createdByUserId, string? tenantOverride = null);

        /// <summary>Actualiza un kiosco existente.</summary>
        Task<KioskDto?> UpdateAsync(Guid id, UpdateKioskRequest request);

        /// <summary>Elimina un kiosco (soft delete: marca IsActive = false).</summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>Actualiza el LastSeenAt del kiosco.</summary>
        Task TouchAsync(Guid kioskId);
    }
}
