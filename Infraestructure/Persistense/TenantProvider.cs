namespace MetrixVox.Infrastructure.Persistence
{
    public interface ITenantContext
    {
        string TenantId { get; }
    }

    // Se llena una vez por petición en TenantMiddleware.
    public class TenantContext : ITenantContext
    {
        public string TenantId { get; set; } = string.Empty;
    }
}
