using System.Text.Json;
using MetrixVox.Application.DTOs;
using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MetrixVox.Application.Services
{
    public record SubmitResult(bool Success, string? Error = null);

    public interface ISurveyIngestionService
    {
        Task<SubmitResult> ProcessAndSaveResponseAsync(SubmitResponseDto dto);
    }

    public class SurveyIngestionService : ISurveyIngestionService
    {
        private const int MaxVerbatimLength = 2000;

        private readonly TenantDbContext _context;
        private readonly IAiAnalyticsService _ai;
        private readonly ILogger<SurveyIngestionService> _logger;

        public SurveyIngestionService(TenantDbContext context, IAiAnalyticsService ai, ILogger<SurveyIngestionService> logger)
        {
            _context = context;
            _ai = ai;
            _logger = logger;
        }

        public async Task<SubmitResult> ProcessAndSaveResponseAsync(SubmitResponseDto dto)
        {
            // 1. Buscar encuesta
            var code = (dto.SurveyCode ?? string.Empty).Trim();
            var survey = await _context.SurveyDefinitions
                .AsNoTracking()
                .Include(s => s.Sections)
                .ThenInclude(sec => sec.Questions)
                .FirstOrDefaultAsync(s => s.SurveyCode == code && s.IsActive);

            if (survey == null)
                return new SubmitResult(false, "La encuesta no existe o no está activa.");

            var verbatim = (dto.SentimentAnalysis?.Verbatim ?? string.Empty).Trim();
            if (verbatim.Length > MaxVerbatimLength)
                return new SubmitResult(false, $"El comentario no puede exceder {MaxVerbatimLength} caracteres.");

            var answers = dto.Metrics ?? new List<MetricAnswerDto>();
            if (answers.Count == 0 && verbatim.Length == 0)
                return new SubmitResult(false, "La respuesta está vacía.");

            // 2. Validar métricas
            var questions = survey.Sections
                .SelectMany(sec => sec.Questions.Select(q => new { Question = q, Category = sec.Title }))
                .ToDictionary(x => x.Question.Id.ToString(), x => x, StringComparer.OrdinalIgnoreCase);

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var metrics = new List<MetricValueRecord>();

            foreach (var answer in answers)
            {
                var key = answer.MetricKey ?? string.Empty;

                if (!questions.TryGetValue(key, out var match))
                    return new SubmitResult(false, $"Pregunta desconocida: {key}");

                if (!seen.Add(key))
                    return new SubmitResult(false, $"Pregunta repetida: {key}");

                if (match.Question.QuestionType == "CSAT_5" && (answer.NumericValue < 1 || answer.NumericValue > 5))
                    return new SubmitResult(false, "Las respuestas de escala deben estar entre 1 y 5.");

                metrics.Add(new MetricValueRecord
                {
                    Category = match.Category,
                    MetricKey = match.Question.Id.ToString(),
                    NumericValue = answer.NumericValue,
                    Weight = 1.0 
                });
            }

            // 3. Promedio
            double totalWeights = metrics.Sum(m => m.Weight);
            double globalScore = totalWeights > 0 ? metrics.Sum(m => m.NumericValue * m.Weight) / totalWeights : 0;

            // 4. IA
            AiInsightResultDto? insight = null;
            if (verbatim.Length > 0)
            {
                try
                {
                    Console.WriteLine($"🤖 IA analizando: {verbatim}");
                    insight = await _ai.AnalyzeVerbatimAsync(verbatim, survey.IndustryType);
                    Console.WriteLine($"✅ IA Respondió: {insight?.Sentiment} - {insight?.DetectedCategory}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔴 ERROR CRÍTICO DE IA: {ex.Message}");
                    _logger.LogError(ex, "Falló el análisis de IA del comentario; se guarda la respuesta sin sentimiento.");
                }
            }

            // 5. Crear Registro
            var isEmployee = string.Equals(survey.Scope, "Employee", StringComparison.OrdinalIgnoreCase);

            var record = new SurveyResponseRecord
            {
                SurveyCode = survey.SurveyCode,
                SubjectId = isEmployee ? null : dto.RespondentContext?.SubjectId,
                BranchOrLocationId = dto.RespondentContext?.BranchOrLocationId ?? string.Empty,
                Channel = dto.RespondentContext?.Channel ?? string.Empty,
                GlobalCalculatedScore = globalScore,
                Verbatim = verbatim.Length > 0 ? verbatim : null,
                
                // Campos de IA
                Sentiment = insight?.Sentiment,
                DetectedCategory = insight?.DetectedCategory,
                AiSummary = insight?.SummaryDescription,
                RequiresImmediateAction = insight?.RequiresImmediateAction ?? false,

                RawPayloadJson = isEmployee ? string.Empty : JsonSerializer.Serialize(dto),
                Metrics = metrics
            };

            _context.SurveyResponseRecords.Add(record);

            if (insight is { RequiresImmediateAction: true })
            {
                _context.AiInsights.Add(new AiInsight
                {
                    SurveyResponseRecordId = record.Id,
                    DetectedCategory = insight.DetectedCategory,
                    VerbatimSnippet = verbatim.Length > 300 ? verbatim[..300] : verbatim,
                    SummaryDescription = insight.SummaryDescription
                });
            }

            await _context.SaveChangesAsync();
            return new SubmitResult(true);
        }
    }
}
