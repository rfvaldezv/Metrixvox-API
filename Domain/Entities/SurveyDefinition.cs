using System;
using System.Collections.Generic;

namespace MetrixVox.Domain.Entities
{
    public class SurveyDefinition : ITenantEntity
    {
        public Guid Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string SurveyCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IndustryType { get; set; } = string.Empty;
        public string Scope { get; set; } = "Customer"; // Customer | Employee
        public bool IsActive { get; set; } = true;
        public ICollection<SurveySection> Sections { get; set; } = new List<SurveySection>();
    }

    public class SurveySection
    {
        public Guid Id { get; set; }
        public Guid SurveyDefinitionId { get; set; }
        public SurveyDefinition? SurveyDefinition { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    }

    public class SurveyQuestion
    {
        public Guid Id { get; set; }
        public Guid SurveySectionId { get; set; }
        public SurveySection? SurveySection { get; set; }
        public string Text { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "CSAT_5";
        public bool IsRequired { get; set; } = true;
        public int Order { get; set; }
    }
}
