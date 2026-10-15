namespace MetrixVox.Domain.Entities
{
    /// <summary>
    /// Roles de usuario en la plataforma MetrixVox.
    /// </summary>
    public enum UserRole
    {
        /// <summary>Dueño de la plataforma SaaS. Acceso a todos los tenants.</summary>
        SuperAdmin = 0,

        /// <summary>Admin de un cliente (tenant). Solo ve su tenant.</summary>
        TenantAdmin = 1,

        /// <summary>Analista de datos. Ve dashboard y reportes.</summary>
        Analyst = 2,

        /// <summary>Supervisor / solo lectura.</summary>
        Viewer = 3,
    }
}
