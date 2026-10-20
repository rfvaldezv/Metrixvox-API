using System;

namespace MetrixVox.Domain.Entities
{
    public class AiReportAnalysis : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TenantId { get; set; } = string.Empty;
        public string SurveyCode { get; set; } = string.Empty;
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int TotalResponses { get; set; }
        public string ExecutiveSummary { get; set; } = string.Empty;
        public string TopProblems { get; set; } = string.Empty; // JSON
        public string GeneralSentiment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
