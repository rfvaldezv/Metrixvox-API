using MetrixVox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/responses")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Analyst,Viewer")]
    public class AdminResponsesController : ControllerBase
    {
        private readonly TenantDbContext _context;

        public AdminResponsesController(TenantDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista las respuestas del tenant activo.
        /// GET /api/v1/admin/responses?limit=100
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int limit = 100)
        {
            var responses = await _context.SurveyResponseRecords
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .Select(r => new
                {
                    r.Id,
                    r.TenantId,
                    r.SurveyCode,
                    r.SubjectId,
                    r.BranchOrLocationId,
                    r.Channel,
                    r.Verbatim,
                    r.Sentiment,
                    r.CreatedAt,
                    MetricCount = r.Metrics.Count(),
                    AvgScore = r.Metrics.Any()
                        ? r.Metrics.Average(m => m.NumericValue)
                        : 0,
                })
                .ToListAsync();

            return Ok(responses);
        }

        /// <summary>
        /// Detalle de una respuesta con sus métricas.
        /// GET /api/v1/admin/responses/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _context.SurveyResponseRecords
                .AsNoTracking()
                .Include(r => r.Metrics)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (response == null) return NotFound();

            return Ok(new
            {
                response.Id,
                response.TenantId,
                response.SurveyCode,
                response.SubjectId,
                response.BranchOrLocationId,
                response.Channel,
                response.Verbatim,
                response.Sentiment,
                response.CreatedAt,
                Metrics = response.Metrics.Select(m => new
                {
                    m.Id,
                    m.Category,
                    m.MetricKey,
                    m.NumericValue,
                    m.Weight,
                }),
            });
        }
    }
}
