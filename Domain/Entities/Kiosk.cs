namespace MetrixVox.Domain.Entities
{
    /// <summary>
    /// Representa una terminal kiosco física.
    /// Cada kiosco se autentica con un KioskToken único y tiene
    /// asignado un tenant + una encuesta.
    /// </summary>
    public class Kiosk : ITenantEntity
    {
        public Guid Id { get; set; }

        /// <summary>Tenant al que pertenece este kiosco.</summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Token único que el kiosco usa para autenticarse.
        /// Se envía como header X-Kiosk-Token.
        /// </summary>
        public string KioskToken { get; set; } = string.Empty;

        /// <summary>Nombre descriptivo (ej. "Sucursal Centro 1").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Sucursal o ubicación (ej. "Sucursal Centro").</summary>
        public string BranchName { get; set; } = string.Empty;

        /// <summary>Código de encuesta que debe mostrar.</summary>
        public string SurveyCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedByUserId { get; set; }

        /// <summary>Última vez que el kiosco se conectó.</summary>
        public DateTime? LastSeenAt { get; set; }
    }
}
