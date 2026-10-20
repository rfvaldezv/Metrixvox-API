using MetrixVox.Application.DTOs;
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
                return Unauthorized(new { error = "Kiosco no encontrado, inactivo o no vinculado." }); // 401 para que la app sepa que debe re-vincularse

            return Ok(config);
        }

        /// <summary>
        /// Vincula una tablet usando el código generado en el Admin.
        /// POST /api/v1/kiosks/pair
        /// </summary>
        [HttpPost("pair")]
        [AllowAnonymous]
        public async Task<IActionResult> PairDevice([FromBody] PairDeviceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var config = await _kioskService.PairDeviceAsync(request.PairingCode);

            if (config == null)
                return BadRequest(new { error = "Código inválido o expirado." });

            return Ok(config);
        }
    }
}
