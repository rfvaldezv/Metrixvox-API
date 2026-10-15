using System.Text.RegularExpressions;
using MetrixVox.Infrastructure.Persistence;

namespace MetrixVox.Api.Middleware
{
    // TEMPORAL: en el paso 2 (seguridad) el tenant saldrá del token JWT y no de un header editable.
    public class TenantMiddleware
    {
        private static readonly Regex ValidTenant = new("^[A-Z0-9_]{3,64}$", RegexOptions.Compiled);
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
        {
            // Las peticiones preflight de CORS no llevan headers personalizados
            if (context.Request.Path.StartsWithSegments("/api") && !HttpMethods.IsOptions(context.Request.Method))
            {
                var tenantId = context.Request.Headers["X-Tenant-Id"].ToString().Trim().ToUpperInvariant();

                // Solo letras, números y guion bajo: evita cualquier valor malicioso
                if (!ValidTenant.IsMatch(tenantId))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { error = "El header X-Tenant-Id es obligatorio y solo admite letras, números y guion bajo (3 a 64 caracteres)." });
                    return;
                }

                tenantContext.TenantId = tenantId;
            }

            await _next(context);
        }
    }
}
