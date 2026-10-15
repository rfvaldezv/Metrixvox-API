using System;

namespace MetrixVox.Domain.Entities
{
    public class AiInsight : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TenantId { get; set; } = string.Empty;
        public Guid? SurveyResponseRecordId { get; set; }
        public string DetectedCategory { get; set; } = string.Empty;
        public string VerbatimSnippet { get; set; } = string.Empty;
        public string SummaryDescription { get; set; } = string.Empty;
        public bool IsResolved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
