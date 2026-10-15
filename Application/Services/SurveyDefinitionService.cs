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

            // El filtro global del contexto ya limita la consulta al tenant activo
            var survey = await _context.SurveyDefinitions
                .AsNoTracking()
                .Include(s => s.Sections)
                .ThenInclude(sec => sec.Questions)
                .FirstOrDefaultAsync(s => s.SurveyCode == code && s.IsActive);

            if (survey == null) return null;

            return new SurveyDto
            {
                SurveyCode = survey.SurveyCode,
                SurveyTitle = survey.Title,
                Scope = survey.Scope,
                Sections = survey.Sections
                    .OrderBy(sec => sec.Order)
                    .Select(sec => new SurveySectionDto
                    {
                        SectionName = sec.Title,
                        Questions = sec.Questions
                            .OrderBy(q => q.Order)
                            .Select(q => new SurveyQuestionDto
                            {
                                Id = q.Id,
                                Text = q.Text,
                                Type = q.QuestionType,
                                IsRequired = q.IsRequired
                            })
                            .ToList()
                    })
                    .ToList()
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
                    NewSection("Atención y Servicio", 1,
                        NewQuestion("¿Cómo califica la atención recibida?", 1),
                        NewQuestion("¿Qué tan probable es que nos recomiende?", 2)),
                    NewSection("Tiempos de Espera", 2,
                        NewQuestion("¿Cómo califica el tiempo de espera?", 1)),
                    NewSection("Instalaciones y Limpieza", 3,
                        NewQuestion("¿Cómo califica la limpieza de las instalaciones?", 1))
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
            Order = order
        };
    }
}
