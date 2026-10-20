using MetrixVox.Application.DTOs;
using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Application.Services
{
    public interface ISurveyDefinitionService
    {
        Task<SurveyDto?> GetSurveyByCodeAsync(string surveyCode);
        Task<bool> SeedDefaultSurveyAsync(string surveyCode, string title, string industryType, string scope = "Customer");
    }

    public class SurveyDefinitionService : ISurveyDefinitionService
    {
        private readonly TenantDbContext _context;

        public SurveyDefinitionService(TenantDbContext context)
        {
            _context = context;
        }

        public async Task<SurveyDto?> GetSurveyByCodeAsync(string surveyCode)
        {
            var code = surveyCode.Trim();

            var survey = await _context.SurveyDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SurveyCode == code && s.IsActive);

            if (survey == null) return null;

            var sections = await _context.SurveySections
                .AsNoTracking()
                .Where(sec => sec.SurveyDefinitionId == survey.Id)
                .OrderBy(sec => sec.Order)
                .ToListAsync();

            var sectionIds = sections.Select(s => s.Id).ToList();
            var questions = await _context.SurveyQuestions
                .AsNoTracking()
                .Where(q => sectionIds.Contains(q.SurveySectionId))
                .OrderBy(q => q.Order)
                .ToListAsync();

            return new SurveyDto
            {
                SurveyCode = survey.SurveyCode,
                SurveyTitle = survey.Title,
                Scope = survey.Scope,
                Sections = sections.Select(sec => new SurveySectionDto
                {
                    SectionName = sec.Title,
                    Questions = questions
                        .Where(q => q.SurveySectionId == sec.Id)
                        .OrderBy(q => q.Order)
                        .Select(q => new SurveyQuestionDto
                        {
                            Id = q.Id,
                            Text = q.Text,
                            Type = q.QuestionType,
                            IsRequired = q.IsRequired,
                            Weight = q.Weight,
                            OptionsJson = q.OptionsJson,
                        })
                        .ToList()
                }).ToList()
            };
        }

        public async Task<bool> SeedDefaultSurveyAsync(string surveyCode, string title, string industryType, string scope = "Customer")
        {
            var exists = await _context.SurveyDefinitions.AnyAsync(s => s.SurveyCode == surveyCode);
            if (exists) return false;

            var survey = new SurveyDefinition
            {
                Id = Guid.NewGuid(),
                SurveyCode = surveyCode,
                Title = title,
                IndustryType = industryType,
                Scope = scope,
                IsActive = true,
                Sections = new List<SurveySection>
                {
                    NewSection("Atencion y Servicio", 1,
                        NewQuestion("Como califica la atencion recibida?", 1),
                        NewQuestion("Que tan probable es que nos recomiende?", 2)),
                    NewSection("Tiempos de Espera", 2,
                        NewQuestion("Como califica el tiempo de espera?", 1)),
                    NewSection("Instalaciones y Limpieza", 3,
                        NewQuestion("Como califica la limpieza de las instalaciones?", 1))
                }
            };

            _context.SurveyDefinitions.Add(survey);
            await _context.SaveChangesAsync();
            return true;
        }

        private static SurveySection NewSection(string title, int order, params SurveyQuestion[] questions) => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Order = order,
            Questions = questions.ToList()
        };

        private static SurveyQuestion NewQuestion(string text, int order) => new()
        {
            Id = Guid.NewGuid(),
            Text = text,
            QuestionType = "CSAT_5",
            IsRequired = true,
            Order = order,
            Weight = 1.0,
            OptionsJson = null
        };
    }
}
