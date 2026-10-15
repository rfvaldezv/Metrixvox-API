using MetrixVox.Application.DTOs;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Application.Services
{
    public interface IDashboardService
    {
        Task<DashboardMetricsDto> GetMetricsAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly TenantDbContext _context;
        private readonly ITenantContext _tenant;

        public DashboardService(TenantDbContext context, ITenantContext tenant)
        {
            _context = context;
            _tenant = tenant;
        }

        public async Task<DashboardMetricsDto> GetMetricsAsync()
        {
            // Todas las consultas quedan limitadas al tenant activo por los filtros globales del contexto
            var total = await _context.SurveyResponseRecords.CountAsync();

            double globalScore = 0;
            if (total > 0)
                globalScore = Math.Round(await _context.SurveyResponseRecords.AverageAsync(r => r.GlobalCalculatedScore), 2);

            var sentimentGroups = await _context.SurveyResponseRecords
                .Where(r => r.Sentiment != null)
                .GroupBy(r => r.Sentiment)
                .Select(g => new { Sentiment = g.Key, Count = g.Count() })
                .ToListAsync();

            int CountOf(string sentiment) => sentimentGroups.FirstOrDefault(g => g.Sentiment == sentiment)?.Count ?? 0;

            var categorySums = await _context.MetricValueRecords
                .GroupBy(m => m.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    WeightedSum = g.Sum(x => x.NumericValue * x.Weight),
                    WeightSum = g.Sum(x => x.Weight)
                })
                .ToListAsync();

            var alerts = await _context.AiInsights
                .Where(a => !a.IsResolved)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new ActionAlertDto
                {
                    InsightId = a.Id,
                    VerbatimSnippet = a.VerbatimSnippet,
                    SummaryDescription = a.SummaryDescription,
                    DetectedCategory = a.DetectedCategory,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return new DashboardMetricsDto
            {
                TenantId = _tenant.TenantId,
                TotalResponses = total,
                GlobalScore = globalScore,
                SentimentBreakdown = new SentimentBreakdownDto
                {
                    PositiveCount = CountOf("Positive"),
                    NeutralCount = CountOf("Neutral"),
                    NegativeCount = CountOf("Negative")
                },
                CategoryPerformances = categorySums
                    .OrderBy(c => c.Category)
                    .Select(c => new CategoryPerformanceDto
                    {
                        CategoryName = c.Category,
                        AverageScore = c.WeightSum > 0 ? Math.Round(c.WeightedSum / c.WeightSum, 2) : 0
                    })
                    .ToList(),
                PendingAlerts = alerts
            };
        }
    }
}
