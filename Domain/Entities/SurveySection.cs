using System;
using System.Collections.Generic;

namespace MetrixVox.Domain.Entities
{
    public class SurveySection
    {
        public Guid Id { get; set; }
        public Guid SurveyDefinitionId { get; set; }
        public SurveyDefinition? SurveyDefinition { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    }
}
