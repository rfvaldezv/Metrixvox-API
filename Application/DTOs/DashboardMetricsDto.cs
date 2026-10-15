namespace MetrixVox.Application.DTOs
{
    public class DashboardMetricsDto
    {
        public string TenantId { get; set; } = string.Empty;
        public int TotalResponses { get; set; }
        public double GlobalScore { get; set; } // Promedio general ponderado
        public SentimentBreakdownDto SentimentBreakdown { get; set; } = new();
        public List<CategoryPerformanceDto> CategoryPerformances { get; set; } = new();
        public List<ActionAlertDto> PendingAlerts { get; set; } = new();
    }

    public class SentimentBreakdownDto
    {
        public int PositiveCount { get; set; }
        public int NeutralCount { get; set; }
        public int NegativeCount { get; set; }
    }

    public class CategoryPerformanceDto
    {
        public string CategoryName { get; set; } = string.Empty; // Ej: TiemposDeEspera, Limpieza, Trato
        public double AverageScore { get; set; }
    }

    public class ActionAlertDto
    {
        public Guid InsightId { get; set; }
        public string VerbatimSnippet { get; set; } = string.Empty;
        public string SummaryDescription { get; set; } = string.Empty;
        public string DetectedCategory { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
