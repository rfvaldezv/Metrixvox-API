using MetrixVox.Application.DTOs;
using MetrixVox.Domain.Entities;
using MetrixVox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MetrixVox.Application.Services
{
    public interface ISurveyManagementService
    {
        Task<List<SurveySummaryDto>> ListAsync();
        Task<SurveyDto?> GetByCodeAsync(string surveyCode);
        Task<SurveyDto> CreateAsync(CreateSurveyRequest request);
        Task<SurveyDto?> UpdateAsync(string surveyCode, CreateSurveyRequest request);
        Task<bool> DeleteAsync(string surveyCode);
    }

    public class SurveyManagementService : ISurveyManagementService
    {
        private readonly TenantDbContext _context;
        private readonly ISurveyDefinitionService _definitionService;

        public SurveyManagementService(
            TenantDbContext context,
            ISurveyDefinitionService definitionService)
        {
            _context = context;
            _definitionService = definitionService;
        }

        public async Task<List<SurveySummaryDto>> ListAsync()
        {
            var surveys = await _context.SurveyDefinitions
                .AsNoTracking()
                .OrderByDescending(s => s.Title)
                .Select(s => new SurveySummaryDto
                {
                    SurveyCode = s.SurveyCode,
                    Title = s.Title,
                    IndustryType = s.IndustryType,
                    Scope = s.Scope,
                    IsActive = s.IsActive,
                    SectionCount = s.Sections.Count(),
                    QuestionCount = s.Sections.SelectMany(sec => sec.Questions).Count(),
                })
                .ToListAsync();

            return surveys;
        }

        public async Task<SurveyDto?> GetByCodeAsync(string surveyCode)
        {
            return await _definitionService.GetSurveyByCodeAsync(surveyCode);
        }

        public async Task<SurveyDto> CreateAsync(CreateSurveyRequest request)
        {
            var code = request.SurveyCode.Trim().ToUpperInvariant();

            var exists = await _context.SurveyDefinitions.AnyAsync(s => s.SurveyCode == code);
            if (exists)
                throw new InvalidOperationException($"Ya existe una encuesta con el código '{code}'.");

            var survey = new SurveyDefinition
            {
                Id = Guid.NewGuid(),
                SurveyCode = code,
                Title = request.SurveyTitle.Trim(),
                IndustryType = request.IndustryType.Trim(),
                Scope = request.Scope.Trim(),
                IsActive = true,
                Sections = request.Sections
                    .OrderBy(s => s.Order)
                    .Select(sec => new SurveySection
                    {
                        Id = Guid.NewGuid(),
                        Title = sec.Title.Trim(),
                        Order = sec.Order,
                        Questions = sec.Questions
                            .OrderBy(q => q.Order)
                            .Select(q => new SurveyQuestion
                            {
                                Id = Guid.NewGuid(),
                                Text = q.Text.Trim(),
                                QuestionType = q.QuestionType.Trim(),
                                IsRequired = q.IsRequired,
                                Order = q.Order,
                                Weight = q.Weight,
                                OptionsJson = q.OptionsJson,
                            })
                            .ToList()
                    })
                    .ToList()
            };

            _context.SurveyDefinitions.Add(survey);
            await _context.SaveChangesAsync();

            return (await GetByCodeAsync(code))!;
        }

        public async Task<SurveyDto?> UpdateAsync(string surveyCode, CreateSurveyRequest request)
        {
            var code = surveyCode.Trim().ToUpperInvariant();

            var survey = await _context.SurveyDefinitions
                .Include(s => s.Sections)
                .ThenInclude(sec => sec.Questions)
                .FirstOrDefaultAsync(s => s.SurveyCode == code);

            if (survey == null) return null;

            // Actualizar campos básicos
            survey.Title = request.SurveyTitle.Trim();
            survey.IndustryType = request.IndustryType.Trim();
            survey.Scope = request.Scope.Trim();

            // Borrar secciones viejas (CASCADE borra preguntas)
            _context.SurveySections.RemoveRange(survey.Sections);

            // Crear secciones nuevas
            survey.Sections = request.Sections
                .OrderBy(s => s.Order)
                .Select(sec => new SurveySection
                {
                    Id = Guid.NewGuid(),
                    SurveyDefinitionId = survey.Id,
                    Title = sec.Title.Trim(),
                    Order = sec.Order,
                    Questions = sec.Questions
                        .OrderBy(q => q.Order)
                        .Select(q => new SurveyQuestion
                        {
                            Id = Guid.NewGuid(),
                            Text = q.Text.Trim(),
                            QuestionType = q.QuestionType.Trim(),
                            IsRequired = q.IsRequired,
                            Order = q.Order,
                            Weight = q.Weight,
                            OptionsJson = q.OptionsJson,
                        })
                        .ToList()
                })
                .ToList();

            await _context.SaveChangesAsync();

            return await GetByCodeAsync(code);
        }

        public async Task<bool> DeleteAsync(string surveyCode)
        {
            var code = surveyCode.Trim().ToUpperInvariant();

            var survey = await _context.SurveyDefinitions
                .FirstOrDefaultAsync(s => s.SurveyCode == code);

            if (survey == null) return false;

            // Soft delete (solo IsActive = false)
            survey.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }

    // DTO para la lista resumida
    public class SurveySummaryDto
    {
        public string SurveyCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IndustryType { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int SectionCount { get; set; }
        public int QuestionCount { get; set; }
    }
}
