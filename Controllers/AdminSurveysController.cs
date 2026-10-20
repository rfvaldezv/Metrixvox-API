using MetrixVox.Application.DTOs;
using MetrixVox.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/surveys")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    public class AdminSurveysController : ControllerBase
    {
        private readonly ISurveyManagementService _service;

        public AdminSurveysController(ISurveyManagementService service)
        {
            _service = service;
        }

        /// <summary>GET /api/v1/admin/surveys</summary>
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var surveys = await _service.ListAsync();
            return Ok(surveys);
        }

        /// <summary>GET /api/v1/admin/surveys/{code}</summary>
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var survey = await _service.GetByCodeAsync(code);
            return survey == null ? NotFound() : Ok(survey);
        }

        /// <summary>POST /api/v1/admin/surveys</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSurveyRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var survey = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetByCode), new { code = survey.SurveyCode }, survey);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>PUT /api/v1/admin/surveys/{code}</summary>
        [HttpPut("{code}")]
        public async Task<IActionResult> Update(string code, [FromBody] CreateSurveyRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var survey = await _service.UpdateAsync(code, request);
            return survey == null ? NotFound() : Ok(survey);
        }

        /// <summary>DELETE /api/v1/admin/surveys/{code}</summary>
        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var deleted = await _service.DeleteAsync(code);
            return deleted ? NoContent() : NotFound();
        }
    }
}
