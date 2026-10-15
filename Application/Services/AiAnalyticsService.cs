using System.Text.RegularExpressions;

namespace MetrixVox.Application.Services
{
    public interface IAiAnalyticsService
    {
        Task<AiInsightResultDto> AnalyzeVerbatimAsync(string verbatim, string industryType);
    }

    // NOTA: implementación TEMPORAL basada en palabras clave (paso 1).
    // En el paso 3 se reemplaza por una llamada real a un modelo de IA, sin cambiar la interfaz.
    public class AiAnalyticsService : IAiAnalyticsService
    {
        private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant;

        private static readonly Regex NegativePattern = new(
            @"\b(tard[oó]|tardaron|demora\w*|demor[oó]|mal[oa]?s?|p[eé]simo\w*|horrible\w*|sucio\w*|lent[oa]s?|nadie|excesiv[oa]s?|queja\w*|grosero\w*|incomod\w*)\b", Opts);

        private static readonly Regex PositivePattern = new(
            @"\b(excelente\w*|buen[oa]s?|muy bien|amable\w*|r[aá]pid[oa]s?|limpi[oa]s?|gracias|perfect[oa]s?|satisfech[oa]s?|content[oa]s?)\b", Opts);

        private static readonly Regex WaitPattern = new(@"\b(espera\w*|demor\w*|tard[oó]\w*|tardaron|cola|fila|caja)\b", Opts);
        private static readonly Regex CleanPattern = new(@"\b(limpi\w*|sucio\w*|ba[ñn][oa]s?|higien\w*)\b", Opts);
        private static readonly Regex ServicePattern = new(@"\b(trato|atenc\w*|atendi\w*|amable\w*|grosero\w*|personal|enfermer\w*|doctor\w*|m[eé]dic\w*)\b", Opts);

        public Task<AiInsightResultDto> AnalyzeVerbatimAsync(string verbatim, string industryType)
        {
            var negatives = NegativePattern.Matches(verbatim).Count;
            var positives = PositivePattern.Matches(verbatim).Count;

            string sentiment = negatives > positives ? "Negative"
                             : positives > negatives ? "Positive"
                             : "Neutral";

            var result = new AiInsightResultDto
            {
                Sentiment = sentiment,
                DetectedCategory = DetectCategory(verbatim),
                SummaryDescription = sentiment switch
                {
                    "Negative" => "Se detectó una experiencia negativa que requiere seguimiento.",
                    "Positive" => "Experiencia satisfactoria reportada por el usuario.",
                    _ => "Comentario sin una valoración clara."
                },
                RequiresImmediateAction = sentiment == "Negative"
            };

            return Task.FromResult(result);
        }

        private static string DetectCategory(string text)
        {
            if (WaitPattern.IsMatch(text)) return "TiemposDeEspera";
            if (CleanPattern.IsMatch(text)) return "Limpieza";
            if (ServicePattern.IsMatch(text)) return "AtencionYServicio";
            return "General";
        }
    }

    public class AiInsightResultDto
    {
        public string Sentiment { get; set; } = string.Empty;
        public string DetectedCategory { get; set; } = string.Empty;
        public string SummaryDescription { get; set; } = string.Empty;
        public bool RequiresImmediateAction { get; set; }
    }
}
