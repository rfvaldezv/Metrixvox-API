using System.ComponentModel.DataAnnotations;

namespace MetrixVox.Application.DTOs
{
    public class CreateKioskRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string BranchName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string SurveyCode { get; set; } = string.Empty;

        /// <summary>
        /// Solo válido para SuperAdmin: permite asignar el kiosco a un tenant específico.
        /// Si es null, se usa el tenant del JWT.
        /// </summary>
        [MaxLength(64)]
        public string? TenantIdOverride { get; set; }
    }

    public class CreateKioskResponse
    {
        public KioskDto Kiosk { get; set; } = new();

        /// <summary>Token único que se debe configurar en la terminal kiosco.</summary>
        public string KioskToken { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }

    public class UpdateKioskRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string BranchName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string SurveyCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
