using System;
using System.Collections.Generic;

namespace MetrixVox.Domain.Entities
{
    public class SurveyResponseRecord : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SurveyCode { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string? SubjectId { get; set; } // Huésped, Paciente o Cliente. En encuestas de empleados siempre queda en null (anónimo)
        public string BranchOrLocationId { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty; // Tablet, QR, Web
        public double GlobalCalculatedScore { get; set; }
        public string? Verbatim { get; set; }   // Comentario libre del encuestado
        public string? Sentiment { get; set; }  // Positive | Neutral | Negative (resultado del análisis de IA)
        public string RawPayloadJson { get; set; } = string.Empty; // Respaldo del JSON recibido (vacío en encuestas de empleados)
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
