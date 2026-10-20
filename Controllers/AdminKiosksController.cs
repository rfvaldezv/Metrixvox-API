using System.Security.Claims;
using MetrixVox.Application.DTOs;
using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/kiosks")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public class AdminKiosksController : ControllerBase
    {
        private readonly IKioskService _kioskService;

        public AdminKiosksController(IKioskService kioskService)
        {
            _kioskService = kioskService;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var kiosks = await _kioskService.ListAsync();
            return Ok(kiosks);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateKioskRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            Guid? userId = null;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(userIdStr, out var parsed)) userId = parsed;

            var role = User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role) ?? "";
            var tenantOverride = role == "SuperAdmin" ? request.TenantIdOverride : null;

            var result = await _kioskService.CreateAsync(request, userId, tenantOverride);
            return CreatedAtAction(nameof(GetById), new { id = result.Kiosk.Id }, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var kiosks = await _kioskService.ListAsync();
            var kiosk = kiosks.FirstOrDefault(k => k.Id == id);
            return kiosk == null ? NotFound() : Ok(kiosk);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateKioskRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _kioskService.UpdateAsync(id, request);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _kioskService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        // --- ENDPOINTS DE PAIRING ---

        /// <summary>POST /api/v1/admin/kiosks/{id}/generate-pairing-code</summary>
        [HttpPost("{id}/generate-pairing-code")]
        public async Task<IActionResult> GeneratePairingCode(Guid id)
        {
            try
            {
                var code = await _kioskService.GeneratePairingCodeAsync(id);
                return Ok(new { pairingCode = code, message = "Código generado. Válido por 15 minutos." });
            }
            catch (KeyNotFoundException) { return NotFound(new { error = "Kiosco no encontrado." }); }
        }

        /// <summary>POST /api/v1/admin/kiosks/{id}/revoke-pairing</summary>
        [HttpPost("{id}/revoke-pairing")]
        public async Task<IActionResult> RevokePairing(Guid id)
        {
            var result = await _kioskService.RevokePairingAsync(id);
            return result ? Ok(new { message = "Vinculación revocada. La tablet deberá vincularse de nuevo." }) : NotFound();
        }
    }
}
