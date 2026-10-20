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
        public string Scope { get; set; } = "Customer";
        public bool IsActive { get; set; } = true;
        public ICollection<SurveySection> Sections { get; set; } = new List<SurveySection>();
    }
}
