using System.ComponentModel.DataAnnotations;

namespace MetrixVox.Application.DTOs
{
    // ==========================================
    // DTO de configuración pública (para la Tablet)
    // ==========================================
    public class KioskConfigDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string SurveyCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string KioskName { get; set; } = string.Empty;
        
        /// <summary>Se llena solo cuando la tablet se vincula por primera vez.</summary>
        public string? KioskToken { get; set; } 
    }

    // ==========================================
    // DTO para listar/ver kioscos en el Admin
    // ==========================================
    public class KioskDto
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string SurveyCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsPaired { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }

    // ==========================================
    // DTO para crear un kiosco (Admin)
    // ==========================================
    public class CreateKioskRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string BranchName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string SurveyCode { get; set; } = string.Empty;

        public string? TenantIdOverride { get; set; }
    }

    public class CreateKioskResponse
    {
        public KioskDto Kiosk { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
    }

    // ==========================================
    // DTO para actualizar un kiosco (Admin)
    // ==========================================
    public class UpdateKioskRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string BranchName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string SurveyCode { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    // ==========================================
    // DTO para el Pairing Flow (Tablet)
    // ==========================================
    public class PairDeviceRequest
    {
        [Required, StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener exactamente 6 caracteres.")]
        public string PairingCode { get; set; } = string.Empty;
    }
}
