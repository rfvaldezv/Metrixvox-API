using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MetrixVox.Application.Services
{
    public interface IAiAnalyticsService
    {
        Task<AiInsightResultDto> AnalyzeVerbatimAsync(string verbatim, string industryType);
    }

    public class AiAnalyticsService : IAiAnalyticsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiAnalyticsService> _logger;
        private readonly string _apiKey;
        // Modelo actualizado a 3.8-flash
        private readonly string _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent";

        public AiAnalyticsService(HttpClient httpClient, IConfiguration config, ILogger<AiAnalyticsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["AiSettings:GeminiApiKey"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_apiKey))
                _logger.LogWarning("⚠️ No se encontró la API Key de Gemini en appsettings.json");
            else
                _logger.LogInformation("✅ API Key de Gemini cargada correctamente.");
        }

        public async Task<AiInsightResultDto> AnalyzeVerbatimAsync(string verbatim, string industryType)
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(verbatim))
            {
                return FallbackAnalysis();
            }

            var prompt = $@"Actúa como un analista experto en Experiencia de Cliente (CX) para la industria '{industryType}'.
Analiza el siguiente comentario de un cliente: ""{verbatim}""

Debes devolver ÚNICAMENTE un objeto JSON válido (sin markdown, sin texto adicional) con esta estructura exacta:
{{
  ""Sentiment"": ""Positive"" | ""Neutral"" | ""Negative"",
  ""DetectedCategory"": ""TiemposDeEspera"" | ""Limpieza"" | ""AtencionYServicio"" | ""Precios"" | ""General"",
  ""SummaryDescription"": ""Una recomendación accionable de máximo 20 palabras para la gerencia."",
  ""RequiresImmediateAction"": true | false
}}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new 
                { 
                    temperature = 0.2, 
                    responseMimeType = "application/json",
                    // Desactivamos el "pensamiento" profundo para que responda en 1-2 segundos
                    thinkingConfig = new { thinkingBudget = 0 } 
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // Sistema de Reintentos
            int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Timeout ampliado a 20 segundos
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var response = await _httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", content, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseJson);
                        var textResponse = doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        if (string.IsNullOrWhiteSpace(textResponse))
                            return FallbackAnalysis();

                        textResponse = textResponse.Replace("```json", "").Replace("```", "").Trim();

                        var result = JsonSerializer.Deserialize<AiInsightResultDto>(textResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return result ?? FallbackAnalysis();
                    }

                    // Reintentar si el servidor de Google está saturado
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || 
                        response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Intento {Attempt}/{Max} falló con {Status}. Reintentando...", attempt, maxRetries, response.StatusCode);
                        
                        if (attempt < maxRetries)
                        {
                            // Espera exponencial: 1s, 2s, 4s...
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
                            continue;
                        }
                    }

                    // Si es otro error (400, 401, 404), no reintentamos
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Error de Gemini ({Status}): {Error}", response.StatusCode, error);
                    return FallbackAnalysis();
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("Intento {Attempt} tardó demasiado (Timeout).", attempt);
                    if (attempt == maxRetries) return FallbackAnalysis();
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado en intento {Attempt}.", attempt);
                    if (attempt == maxRetries) return FallbackAnalysis();
                    await Task.Delay(1000);
                }
            }

            return FallbackAnalysis();
        }

        private static AiInsightResultDto FallbackAnalysis()
        {
            return new AiInsightResultDto
            {
                Sentiment = "Neutral",
                DetectedCategory = "General",
                SummaryDescription = "Análisis automático no disponible en este momento.",
                RequiresImmediateAction = false
            };
        }
    }

    public class AiInsightResultDto
    {
        [JsonPropertyName("Sentiment")]
        public string Sentiment { get; set; } = string.Empty;

        [JsonPropertyName("DetectedCategory")]
        public string DetectedCategory { get; set; } = string.Empty;

        [JsonPropertyName("SummaryDescription")]
        public string SummaryDescription { get; set; } = string.Empty;

        [JsonPropertyName("RequiresImmediateAction")]
        public bool RequiresImmediateAction { get; set; }
    }
}
