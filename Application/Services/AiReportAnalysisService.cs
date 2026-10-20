using System.Text;
using System.Text.Json;
using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MetrixVox.Application.Services
{
    public interface IAiReportAnalysisService
    {
        Task<AiReportAnalysis> GenerateReportAsync(string tenantId, string surveyCode, DateTime from, DateTime to);
        Task<List<AiReportAnalysis>> GetHistoryAsync(string tenantId);
    }

    public class AiReportAnalysisService : IAiReportAnalysisService
    {
        private readonly TenantDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<AiReportAnalysisService> _logger;
        private readonly string _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";

        public AiReportAnalysisService(TenantDbContext context, HttpClient httpClient, IConfiguration config, ILogger<AiReportAnalysisService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<List<AiReportAnalysis>> GetHistoryAsync(string tenantId)
        {
            return await _context.AiReportAnalyses
                .IgnoreQueryFilters()
                .Where(a => a.TenantId == tenantId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<AiReportAnalysis> GenerateReportAsync(string tenantId, string surveyCode, DateTime from, DateTime to)
        {
            var query = _context.SurveyResponseRecords
                .IgnoreQueryFilters()
                .Where(r => r.TenantId == tenantId && r.SurveyCode == surveyCode && r.CreatedAt >= from && r.CreatedAt <= to);

            var totalCount = await query.CountAsync();
            if (totalCount < 30)
            {
                throw new InvalidOperationException($"Muestra insuficiente. Se necesitan al menos 30 respuestas y solo hay {totalCount}.");
            }

            var comments = await query
                .Where(r => !string.IsNullOrEmpty(r.Verbatim))
                .Select(r => r.Verbatim)
                .ToListAsync();

            if (comments.Count == 0)
                throw new InvalidOperationException("No hay comentarios de texto en el periodo seleccionado.");

            var commentsText = string.Join("\n- ", comments.Take(500));
            var prompt = $@"Actúa como un Consultor Senior de Experiencia de Cliente (CX).
Aquí tienes una lista de {totalCount} respuestas de clientes (comentarios):
- {commentsText}

Analiza TODOS estos comentarios y genera un informe ejecutivo. Devuelve ÚNICAMENTE un JSON válido (sin markdown) con esta estructura:
{{
  ""GeneralSentiment"": ""Positive"" | ""Neutral"" | ""Negative"",
  ""ExecutiveSummary"": ""Un párrafo de 3-4 oraciones con las conclusiones generales y recomendaciones clave para la gerencia."",
  ""TopProblems"": [
    {{ ""Issue"": ""Nombre del problema 1"", ""Percentage"": 45, ""Recommendation"": ""Acción sugerida"" }},
    {{ ""Issue"": ""Nombre del problema 2"", ""Percentage"": 30, ""Recommendation"": ""Acción sugerida"" }},
    {{ ""Issue"": ""Nombre del problema 3"", ""Percentage"": 15, ""Recommendation"": ""Acción sugerida"" }}
  ]
}}";

            var apiKey = _config["AiSettings:GeminiApiKey"] ?? string.Empty;
            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.3, responseMimeType = "application/json" }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var response = await _httpClient.PostAsync($"{_apiUrl}?key={apiKey}", content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error de Gemini en reporte: {Err}", err);
                throw new Exception("La IA no pudo generar el reporte en este momento. Intenta de nuevo más tarde.");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var textResponse = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            textResponse = textResponse?.Replace("```json", "").Replace("```", "").Trim();

            using var resultDoc = JsonDocument.Parse(textResponse!);
            
            var analysis = new AiReportAnalysis
            {
                TenantId = tenantId,
                SurveyCode = surveyCode,
                DateFrom = from,
                DateTo = to,
                TotalResponses = totalCount,
                GeneralSentiment = resultDoc.RootElement.GetProperty("GeneralSentiment").GetString() ?? "Neutral",
                ExecutiveSummary = resultDoc.RootElement.GetProperty("ExecutiveSummary").GetString() ?? "",
                TopProblems = resultDoc.RootElement.GetProperty("TopProblems").GetRawText()
            };

            _context.AiReportAnalyses.Add(analysis);
            await _context.SaveChangesAsync();

            return analysis;
        }
    }
}
