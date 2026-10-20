using MetrixVox.Application.Reports;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Application.Services
{
    public class AdminReportsService
    {
        private readonly TenantDbContext _context;

        public AdminReportsService(TenantDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GeneratePdfByIdAsync(Guid analysisId)
        {
            var analysis = await _context.AiReportAnalyses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == analysisId);

            if (analysis == null) throw new Exception("Análisis no encontrado.");

            // Buscamos el nombre del cliente (Tenant) para la portada
            var tenantName = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.TenantId == analysis.TenantId)
		.Select(t => t.TenantId)
                .FirstOrDefaultAsync() ?? analysis.TenantId;

            var reportData = await BuildReportDataAsync(analysis.TenantId, tenantName, analysis.SurveyCode, analysis.DateFrom, analysis.DateTo);
            return ReportGenerator.GenerateExecutiveReport(reportData, analysis);
        }

        public async Task<byte[]> GeneratePdfAsync(string tenantId, string surveyCode, DateTime from, DateTime to)
        {
            var tenantName = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.TenantId == tenantId)
		.Select(t => t.TenantId)
                .FirstOrDefaultAsync() ?? tenantId;

            var reportData = await BuildReportDataAsync(tenantId, tenantName, surveyCode, from, to);
            
            var aiAnalysis = await _context.AiReportAnalyses
                .IgnoreQueryFilters()
                .Where(a => a.TenantId == tenantId && a.SurveyCode == surveyCode)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            return ReportGenerator.GenerateExecutiveReport(reportData, aiAnalysis);
        }

        private async Task<ReportData> BuildReportDataAsync(string tenantId, string tenantName, string surveyCode, DateTime from, DateTime to)
        {
            var query = _context.SurveyResponseRecords
                .IgnoreQueryFilters()
                .Where(r => r.TenantId == tenantId && r.SurveyCode == surveyCode && r.CreatedAt >= from && r.CreatedAt <= to);

            var responses = await query.ToListAsync();

            var data = new ReportData
            {
                TenantName = tenantName,
                Period = $"{from:dd/MM/yyyy} al {to:dd/MM/yyyy}",
                TotalResponses = responses.Count,
                GlobalScore = responses.Any() ? responses.Average(r => r.GlobalCalculatedScore) : 0,
                PositiveCount = responses.Count(r => r.GlobalCalculatedScore >= 4),
                NeutralCount = responses.Count(r => r.GlobalCalculatedScore >= 3 && r.GlobalCalculatedScore < 4),
                NegativeCount = responses.Count(r => r.GlobalCalculatedScore < 3),
                PendingAlerts = responses.Count(r => r.RequiresImmediateAction)
            };

            // Top Verbatims (los últimos 10 con texto)
            data.TopVerbatims = responses
                .Where(r => !string.IsNullOrEmpty(r.Verbatim))
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new VerbatimData
                {
                    Text = r.Verbatim!,
                    Branch = r.BranchOrLocationId,
                    Sentiment = r.Sentiment ?? "Neutral",
                    Date = r.CreatedAt
                }).ToList();

            return data;
        }
    }
}
