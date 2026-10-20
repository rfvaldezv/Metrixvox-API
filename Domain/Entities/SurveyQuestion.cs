namespace MetrixVox.Domain.Entities
{
    public class SurveyQuestion
    {
        public Guid Id { get; set; }
        public Guid SurveySectionId { get; set; }
        public SurveySection? SurveySection { get; set; }

        public string Text { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "CSAT_5"; // CSAT_5, YES_NO, NPS_10, MULTIPLE_CHOICE, TEXT
        public bool IsRequired { get; set; } = true;
        public int Order { get; set; }
        
        // 🆕 Peso de la pregunta para el cálculo del score
        public double Weight { get; set; } = 1.0;
        
        // 🆕 Opciones en formato JSON (ej. ["Sí", "No"] o ["A", "B", "C"])
        public string? OptionsJson { get; set; }
    }
}
