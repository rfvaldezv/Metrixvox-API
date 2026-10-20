namespace MetrixVox.Application.DTOs
{
    public class SurveyDto
    {
        public string SurveyCode { get; set; } = string.Empty;
        public string SurveyTitle { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public List<SurveySectionDto> Sections { get; set; } = new();
    }

    public class SurveySectionDto
    {
        public string SectionName { get; set; } = string.Empty;
        public List<SurveyQuestionDto> Questions { get; set; } = new();
    }

    public class SurveyQuestionDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = "CSAT_5";
        public bool IsRequired { get; set; }
        public double Weight { get; set; }
        public string? OptionsJson { get; set; }
    }
}
