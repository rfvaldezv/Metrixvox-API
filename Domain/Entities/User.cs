namespace MetrixVox.Domain.Entities
{
    /// <summary>
    /// Usuario de la plataforma. Puede ser SuperAdmin (sin tenant)
    /// o pertenecer a un tenant específico (TenantAdmin, Analyst, Viewer).
    /// </summary>
    public class User : ITenantEntity
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Tenant al que pertenece el usuario.
        /// Para SuperAdmin será un valor especial (ej. "__SUPER__").
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Viewer;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }
}
