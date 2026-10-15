namespace MetrixVox.Application.DTOs
{
    /// <summary>Información completa de un kiosco (para el panel admin).</summary>
    public class KioskDto
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string SurveyCode { get; set; } = string.Empty;
        public string KioskToken { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }

    /// <summary>Configuración mínima que el kiosco necesita para operar.</summary>
    public class KioskConfigDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string SurveyCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string KioskName { get; set; } = string.Empty;
    }
}
