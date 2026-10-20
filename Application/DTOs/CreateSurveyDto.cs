using System.ComponentModel.DataAnnotations;

namespace MetrixVox.Application.DTOs
{
    public class CreateSurveyRequest
    {
        [Required, MaxLength(50)]
        public string SurveyCode { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string SurveyTitle { get; set; } = string.Empty;

        [MaxLength(50)]
        public string IndustryType { get; set; } = "Generic";

        [MaxLength(20)]
        public string Scope { get; set; } = "Customer";

        public List<CreateSectionRequest> Sections { get; set; } = new();
    }

    public class CreateSectionRequest
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public int Order { get; set; } = 1;

        public List<CreateQuestionRequest> Questions { get; set; } = new();
    }

    public class CreateQuestionRequest
    {
        [Required, MaxLength(500)]
        public string Text { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string QuestionType { get; set; } = "CSAT_5";

        public bool IsRequired { get; set; } = true;

        public int Order { get; set; } = 1;

        public double Weight { get; set; } = 1.0;

        /// <summary>JSON con las opciones (ej. ["Sí", "No"] para YES_NO, ["A","B","C"] para MULTIPLE_CHOICE)</summary>
        public string? OptionsJson { get; set; }
    }
}
