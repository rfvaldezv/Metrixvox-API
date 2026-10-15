namespace MetrixVox.Domain.Entities
{
    public class TenantDatabaseRecord
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = string.Empty; // Ej: CLINICA_OFTALMOLOGIA
        public string DatabaseName { get; set; } = string.Empty; // Ej: MetrixVox_CLINICA_OFTALMOLOGIA
        public string ServerName { get; set; } = string.Empty; // Instancia de SQL Server en el VPS
        public string Status { get; set; } = "Pending"; // Pending, Ready, Error
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
