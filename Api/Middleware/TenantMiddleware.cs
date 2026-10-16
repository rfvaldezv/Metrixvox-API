using System.Text.RegularExpressions;
using MetrixVox.Infrastructure.Persistence;

namespace MetrixVox.Api.Middleware
{
    /// <summary>
    /// Resuelve el TenantId activo para cada request.
    /// Fuentes, en orden de prioridad:
    ///   1. SuperAdmin + header X-Tenant-Id → impersona el tenant del header
    ///   2. Claim "tenantId" del JWT → tenant propio del usuario
    ///   3. Header X-Tenant-Id → kioscos / legacy sin JWT
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

            var headerTenant = context.Request.Headers["X-Tenant-Id"].ToString().Trim().ToUpperInvariant();
            var hasHeader = !string.IsNullOrWhiteSpace(headerTenant);

            // ¿JWT presente?
            var jwtTenant = context.User?.FindFirst("tenantId")?.Value;
	    var userRole = context.User?.FindFirst("role")?.Value
	        ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var hasJwt = !string.IsNullOrWhiteSpace(jwtTenant);

            if (hasJwt)
            {
                // SuperAdmin puede impersonar un tenant con X-Tenant-Id
                if (userRole == "SuperAdmin" && hasHeader)
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
                    await _next(context);
                    return;
                }

                // Usuario normal: usar el tenant del JWT (no puede cambiar)
                tenantContext.TenantId = jwtTenant!.Trim().ToUpperInvariant();
                await _next(context);
                return;
            }

            // Sin JWT: usar el header X-Tenant-Id (kioscos / legacy)
            if (hasHeader)
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
