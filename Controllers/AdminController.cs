using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ISurveyDefinitionService _surveyService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ISurveyDefinitionService surveyService,
            ILogger<AdminController> logger)
        {
            _surveyService = surveyService;
            _logger = logger;
        }

        /// <summary>
        /// Siembra la encuesta por defecto para el tenant indicado en el header X-Tenant-Id.
        /// POST /api/v1/admin/seed-survey
        /// Headers: X-Tenant-Id: HOSPITAL_SAN_JOSE
        /// Body: { "surveyCode": "SURVEY_HOSPITAL_01", "title": "...", "industryType": "Healthcare" }
        /// 
        /// ⚠️ TEMPORAL sin autenticación. En el paso 2 (seguridad) se protegerá con JWT + rol Admin.
        /// </summary>
        [HttpPost("seed-survey")]
        public async Task<IActionResult> SeedSurvey([FromBody] SeedSurveyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SurveyCode))
                return BadRequest(new { error = "surveyCode es obligatorio." });

            _logger.LogInformation(
                "Seed solicitado: SurveyCode={SurveyCode}, Tenant={Tenant}",
                request.SurveyCode,
                HttpContext.Request.Headers["X-Tenant-Id"].ToString());

            var created = await _surveyService.SeedDefaultSurveyAsync(
                request.SurveyCode,
                request.Title ?? request.SurveyCode,
                request.IndustryType ?? "Generic",
                request.Scope ?? "Customer"
            );

            return Ok(new
            {
                created,
                message = created
                    ? $"Encuesta '{request.SurveyCode}' creada correctamente."
                    : $"La encuesta '{request.SurveyCode}' ya existía para este tenant."
            });
        }
    }

    public class SeedSurveyRequest
    {
        public string SurveyCode { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? IndustryType { get; set; }
        public string? Scope { get; set; }
    }
}
