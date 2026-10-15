using MetrixVox.Application.DTOs;
using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/surveys")]
    public class SurveysController : ControllerBase
    {
        private readonly ISurveyDefinitionService _definitions;
        private readonly ISurveyIngestionService _ingestion;

        public SurveysController(ISurveyDefinitionService definitions, ISurveyIngestionService ingestion)
        {
            _definitions = definitions;
            _ingestion = ingestion;
        }

        [HttpGet("{surveyCode}")]
        public async Task<IActionResult> GetSurveyByCode(string surveyCode)
        {
            var survey = await _definitions.GetSurveyByCodeAsync(surveyCode);

            return survey is null
                ? NotFound(new { error = "Encuesta no encontrada para este cliente." })
                : Ok(survey);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] SubmitResponseDto dto)
        {
            var result = await _ingestion.ProcessAndSaveResponseAsync(dto);

            return result.Success
                ? Ok(new { success = true })
                : BadRequest(new { success = false, error = result.Error });
        }
    }
}
