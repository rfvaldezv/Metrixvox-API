using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/analytics")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public class AdminAnalyticsController : ControllerBase
    {
        private readonly IAiReportAnalysisService _analysisService;

        public AdminAnalyticsController(IAiReportAnalysisService analysisService)
        {
            _analysisService = analysisService;
        }

        public class GenerateAnalysisRequest
        {
            public string TenantId { get; set; } = string.Empty;
            public string SurveyCode { get; set; } = string.Empty;
            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GenerateAnalysisRequest request)
        {
            try
            {
                var result = await _analysisService.GenerateReportAsync(request.TenantId, request.SurveyCode, request.DateFrom, request.DateTo);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] string tenantId)
        {
            var history = await _analysisService.GetHistoryAsync(tenantId);
            return Ok(history);
        }
    }
}
