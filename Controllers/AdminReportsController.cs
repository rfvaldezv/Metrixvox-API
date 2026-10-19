using MetrixVox.Application.Reports;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin/reports")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Analyst")]
    public class AdminReportsController : ControllerBase
    {
        private readonly TenantDbContext _context;
        private readonly ILogger<AdminReportsController> _logger;

        public AdminReportsController(TenantDbContext context, ILogger<AdminReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Genera un reporte PDF ejecutivo del tenant activo.
        /// GET /api/v1/admin/reports/executive
        /// </summary>
        [HttpGet("executive")]
        public async Task<IActionResult> GenerateExecutiveReport()
        {
            var tenantId = GetActiveTenant();
            if (string.IsNullOrWhiteSpace(tenantId))
                return BadRequest(new { error = "No hay tenant activo." });

            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);
            var tenantName = tenant?.DisplayName ?? tenantId;

            var responses = await _context.SurveyResponseRecords
                .AsNoTracking()
                .ToListAsync();

            var metrics = await _context.MetricValueRecords
                .AsNoTracking()
                .Include(m => m.SurveyResponseRecord)
                .ToListAsync();

            var insights = await _context.AiInsights
                .AsNoTracking()
                .Where(i => !i.IsResolved)
                .ToListAsync();

            var totalResponses = responses.Count;
            var globalScore = metrics.Any() ? metrics.Average(m => m.NumericValue) : 0;

            var sentimentGroups = responses
                .GroupBy(r => r.Sentiment ?? "Neutral")
                .ToDictionary(g => g.Key, g => g.Count());

            var categories = metrics
                .GroupBy(m => m.Category)
                .Select(g => new CategoryData
                {
                    Name = g.Key,
                    Score = g.Average(m => m.NumericValue),
                })
                .OrderByDescending(c => c.Score)
                .ToList();

            var topVerbatims = responses
                .Where(r => !string.IsNullOrWhiteSpace(r.Verbatim))
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new VerbatimData
                {
                    Text = r.Verbatim ?? "",
                    Branch = r.BranchOrLocationId ?? "—",
                    Sentiment = r.Sentiment ?? "Neutral",
                    Date = r.CreatedAt,
                })
                .ToList();

            var executiveSummary = GenerateExecutiveSummary(
                totalResponses, globalScore, categories, sentimentGroups);

            var recommendations = GenerateRecommendations(categories, globalScore);

            var data = new ReportData
            {
                TenantName = tenantName,
                Period = $"{DateTime.UtcNow:MMMM yyyy}",
                TotalResponses = totalResponses,
                GlobalScore = globalScore,
                PositiveCount = sentimentGroups.GetValueOrDefault("Positive", 0),
                NeutralCount = sentimentGroups.GetValueOrDefault("Neutral", 0),
                NegativeCount = sentimentGroups.GetValueOrDefault("Negative", 0),
                PendingAlerts = insights.Count,
                Categories = categories,
                TopVerbatims = topVerbatims,
                ExecutiveSummary = executiveSummary,
                Recommendations = recommendations,
            };

            var pdfBytes = ReportGenerator.GenerateExecutiveReport(data);

            var filename = $"Reporte_{tenantId}_{DateTime.Now:yyyy-MM-dd}.pdf";

            _logger.LogInformation("Reporte PDF generado para {Tenant}, {Bytes} bytes", tenantId, pdfBytes.Length);

            return File(pdfBytes, "application/pdf", filename);
        }

        private string GetActiveTenant()
        {
            var header = Request.Headers["X-Tenant-Id"].ToString().Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(header)) return header;

            var jwtTenant = User.FindFirst("tenantId")?.Value;
            return jwtTenant ?? string.Empty;
        }

        private static string GenerateExecutiveSummary(
            int total, double score, List<CategoryData> categories, Dictionary<string, int> sentiment)
        {
            var positive = sentiment.GetValueOrDefault("Positive", 0);
            var negative = sentiment.GetValueOrDefault("Negative", 0);
            var best = categories.FirstOrDefault();
            var worst = categories.LastOrDefault();

            var summary = $"Durante este período se capturaron {total} encuestas de experiencia de cliente. ";
            summary += $"El score global de satisfacción fue de {score:0.00}/5.0. ";

            if (positive > 0 && total > 0)
                summary += $"El {positive * 100 / total}% de las respuestas fueron positivas. ";

            if (negative > 0 && total > 0)
                summary += $"Se detectaron {negative} quejas que requieren atención. ";

            if (best != null)
                summary += $"La categoría mejor evaluada fue '{best.Name}' con {best.Score:0.00}/5.0. ";

            if (worst != null && worst.Name != best?.Name)
                summary += $"La categoría con mayor oportunidad de mejora es '{worst.Name}' con {worst.Score:0.00}/5.0.";

            return summary;
        }

        private static List<string> GenerateRecommendations(List<CategoryData> categories, double score)
        {
            var recs = new List<string>();

            if (categories.Any())
            {
                var worst = categories.Last();
                if (worst.Score < 3.5)
                {
                    recs.Add($"Implementar plan de mejora en '{worst.Name}' — actualmente con {worst.Score:0.00}/5.0. " +
                             "Realizar sesiones de capacitación y revisar procesos internos.");
                }

                var best = categories.First();
                recs.Add($"Mantener y replicar las mejores prácticas de '{best.Name}' " +
                         $"({best.Score:0.00}/5.0) en otras áreas de la organización.");
            }

            if (score < 3.5)
                recs.Add("Reforzar la atención al cliente y reducir tiempos de espera para elevar el score global.");
            else if (score >= 4.0)
                recs.Add("Continuar con las estrategias actuales — el score global indica satisfacción alta.");

            recs.Add("Analizar los comentarios de los clientes mensualmente para identificar patrones y tendencias.");
            recs.Add("Considerar implementar encuestas de seguimiento (NPS) para medir lealtad a largo plazo.");

            return recs;
        }
    }
}
