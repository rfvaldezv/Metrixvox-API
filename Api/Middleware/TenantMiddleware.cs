using System.Text.RegularExpressions;
using MetrixVox.Infrastructure.Persistence;

namespace MetrixVox.Api.Middleware
{
    /// <summary>
    /// Resuelve el TenantId activo para cada request.
    /// Fuentes, en orden de prioridad:
    ///   1. Claim "tenantId" del JWT (usuarios autenticados)
    ///   2. Header X-Tenant-Id (kioscos / legacy)
    /// </summary>
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
            // Preflight de CORS no lleva headers personalizados
            if (!context.Request.Path.StartsWithSegments("/api") ||
                HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // 1) ¿JWT con claim tenantId?
            var jwtTenant = context.User?.FindFirst("tenantId")?.Value;
            if (!string.IsNullOrWhiteSpace(jwtTenant))
            {
                tenantContext.TenantId = jwtTenant.Trim().ToUpperInvariant();
                await _next(context);
                return;
            }

            // 2) Header X-Tenant-Id (kioscos / legacy)
            var headerTenant = context.Request.Headers["X-Tenant-Id"].ToString().Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(headerTenant))
            {
                if (!ValidTenant.IsMatch(headerTenant))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "El header X-Tenant-Id solo admite letras, números y guion bajo (3-64 caracteres)."
                    });
                    return;
                }

                tenantContext.TenantId = headerTenant;
            }

            await _next(context);
        }
    }
}
