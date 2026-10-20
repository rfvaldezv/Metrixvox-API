using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/reports")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public class AdminReportsController : ControllerBase
    {
        private readonly AdminReportsService _reportsService;

        public AdminReportsController(AdminReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [HttpGet("executive")]
        public async Task<IActionResult> GenerateExecutiveReport(
            [FromQuery] string tenantId, [FromQuery] string surveyCode, [FromQuery] DateTime dateFrom, [FromQuery] DateTime dateTo)
        {
            try
            {
                var pdfBytes = await _reportsService.GeneratePdfAsync(tenantId, surveyCode, dateFrom, dateTo);
                return File(pdfBytes, "application/pdf", $"Reporte_Ejecutivo_{tenantId}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("executive/{id}")]
        public async Task<IActionResult> DownloadById(Guid id)
        {
            try
            {
                var pdfBytes = await _reportsService.GeneratePdfByIdAsync(id);
                return File(pdfBytes, "application/pdf", $"Reporte_Ejecutivo_{id}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
