using System.Collections.Generic;

namespace MetrixVox.Application.DTOs
{
    public class SubmitResponseDto
    {
        public string TenantId { get; set; } = string.Empty;
        public string SurveyCode { get; set; } = string.Empty;
        public RespondentContextDto RespondentContext { get; set; } = new();
        public List<MetricAnswerDto> Metrics { get; set; } = new();
        public SentimentInputDto SentimentAnalysis { get; set; } = new();
    }

    public class RespondentContextDto
    {
        public string? SubjectId { get; set; }
        public string BranchOrLocationId { get; set; } = string.Empty;
        public string Channel { get; set; } = "TABLET";
    }

    public class MetricAnswerDto
    {
        public string Category { get; set; } = string.Empty;
        public string MetricKey { get; set; } = string.Empty;
        public double NumericValue { get; set; }
        public double Weight { get; set; } = 1.0;
    }

    public class SentimentInputDto
    {
        public string Verbatim { get; set; } = string.Empty;
    }
}
