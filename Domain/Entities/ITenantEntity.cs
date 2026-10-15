namespace MetrixVox.Domain.Entities
{
    // Toda entidad que pertenece a un cliente (tenant) implementa esta interfaz.
    // El DbContext la usa para filtrar automáticamente y para estampar el TenantId al guardar.
    public interface ITenantEntity
    {
        string TenantId { get; set; }
    }
}
