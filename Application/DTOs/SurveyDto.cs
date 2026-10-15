namespace MetrixVox.Application.DTOs
{
    // Estructura que consume la App Flutter (nombres en camelCase al serializarse)
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
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
    }
}
