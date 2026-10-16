namespace MetrixVox.Domain.Entities
{
    /// <summary>
    /// Tenant (Cliente) registrado en la plataforma.
    /// El TenantId es el código único que se usa en las demás tablas.
    /// </summary>
    public class Tenant
    {
        public Guid Id { get; set; }

        /// <summary>Código único (ej. "HOSPITAL_SAN_JOSE").</summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>Nombre amigable para mostrar (ej. "Hospital San José").</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Email de contacto del cliente.</summary>
        public string? ContactEmail { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
