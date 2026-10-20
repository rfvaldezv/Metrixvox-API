using System;
using System.Collections.Generic;

namespace MetrixVox.Domain.Entities
{
    public class SurveyResponseRecord : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SurveyCode { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string? SubjectId { get; set; }
        public string BranchOrLocationId { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public double GlobalCalculatedScore { get; set; }
        public string? Verbatim { get; set; }
        
        // --- CAMPOS DE IA ---
        public string? Sentiment { get; set; }
        public string? DetectedCategory { get; set; } // NUEVO: Categoría detectada (ej. TiemposDeEspera)
        public string? AiSummary { get; set; }        // NUEVO: Resumen/Recomendación de la IA
        public bool RequiresImmediateAction { get; set; } // NUEVO: Bandera para alertas

        public string RawPayloadJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<MetricValueRecord> Metrics { get; set; } = new List<MetricValueRecord>();
    }

    public class MetricValueRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SurveyResponseRecordId { get; set; }
        public SurveyResponseRecord? SurveyResponseRecord { get; set; }
        public string Category { get; set; } = string.Empty;
        public string MetricKey { get; set; } = string.Empty;
        public double NumericValue { get; set; }
        public double Weight { get; set; } = 1.0;
    }
}
