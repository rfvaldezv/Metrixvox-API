using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/kiosks")]
    public class KiosksController : ControllerBase
    {
        private readonly IKioskService _kioskService;

        public KiosksController(IKioskService kioskService)
        {
            _kioskService = kioskService;
        }

        /// <summary>
        /// Devuelve la configuración del kiosco autenticado por su token.
        /// GET /api/v1/kiosks/me
        /// Headers: X-Kiosk-Token: &lt;token&gt;
        /// </summary>
        [HttpGet("me")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMyConfig()
        {
            var token = Request.Headers["X-Kiosk-Token"].ToString().Trim();

            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { error = "El header X-Kiosk-Token es obligatorio." });

            var config = await _kioskService.GetByTokenAsync(token);

            if (config == null)
                return NotFound(new { error = "Kiosco no encontrado o inactivo." });

            return Ok(config);
        }
    }
}
