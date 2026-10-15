using Microsoft.EntityFrameworkCore;
using MetrixVox.Domain.Entities;

namespace MetrixVox.Infrastructure.Persistence
{
    public class TenantDbContext : DbContext
    {
        private readonly ITenantContext _tenantContext;

        public TenantDbContext(DbContextOptions<TenantDbContext> options, ITenantContext tenantContext)
            : base(options)
        {
            _tenantContext = tenantContext;
        }

        // Los filtros globales leen esta propiedad en cada consulta (por instancia de contexto).
        private string CurrentTenantId => _tenantContext.TenantId;

        public DbSet<SurveyDefinition> SurveyDefinitions { get; set; }
        public DbSet<SurveySection> SurveySections { get; set; }
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
        public DbSet<SurveyResponseRecord> SurveyResponseRecords { get; set; }
        public DbSet<MetricValueRecord> MetricValueRecords { get; set; }
        public DbSet<AiInsight> AiInsights { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SurveyDefinition>(e =>
            {
                e.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
                e.Property(x => x.SurveyCode).HasMaxLength(50).IsRequired();
                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
                e.Property(x => x.IndustryType).HasMaxLength(50);
                e.Property(x => x.Scope).HasMaxLength(20).IsRequired();
                e.HasIndex(x => new { x.TenantId, x.SurveyCode }).IsUnique();
                e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            });

            modelBuilder.Entity<SurveySection>(e =>
            {
                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
                e.HasOne(x => x.SurveyDefinition)
                    .WithMany(x => x.Sections)
                    .HasForeignKey(x => x.SurveyDefinitionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasQueryFilter(x => x.SurveyDefinition!.TenantId == CurrentTenantId);
            });

            modelBuilder.Entity<SurveyQuestion>(e =>
            {
                e.Property(x => x.Text).HasMaxLength(500).IsRequired();
                e.Property(x => x.QuestionType).HasMaxLength(30).IsRequired();
                e.HasOne(x => x.SurveySection)
                    .WithMany(x => x.Questions)
                    .HasForeignKey(x => x.SurveySectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasQueryFilter(x => x.SurveySection!.SurveyDefinition!.TenantId == CurrentTenantId);
            });

            modelBuilder.Entity<SurveyResponseRecord>(e =>
            {
                e.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
                e.Property(x => x.SurveyCode).HasMaxLength(50).IsRequired();
                e.Property(x => x.SubjectId).HasMaxLength(100);
                e.Property(x => x.BranchOrLocationId).HasMaxLength(100);
                e.Property(x => x.Channel).HasMaxLength(30);
                e.Property(x => x.Verbatim).HasMaxLength(2000);
                e.Property(x => x.Sentiment).HasMaxLength(20);
                e.HasIndex(x => new { x.TenantId, x.CreatedAt });
                e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            });

            modelBuilder.Entity<MetricValueRecord>(e =>
            {
                e.Property(x => x.Category).HasMaxLength(200).IsRequired();
                e.Property(x => x.MetricKey).HasMaxLength(100).IsRequired();
                e.HasOne(x => x.SurveyResponseRecord)
                    .WithMany(x => x.Metrics)
                    .HasForeignKey(x => x.SurveyResponseRecordId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasQueryFilter(x => x.SurveyResponseRecord!.TenantId == CurrentTenantId);
            });

            modelBuilder.Entity<AiInsight>(e =>
            {
                e.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
                e.Property(x => x.DetectedCategory).HasMaxLength(100);
                e.Property(x => x.VerbatimSnippet).HasMaxLength(300);
                e.Property(x => x.SummaryDescription).HasMaxLength(500);
                e.HasIndex(x => new { x.TenantId, x.IsResolved, x.CreatedAt });
                e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            });
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampTenant();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampTenant();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // Garantiza que todo registro nuevo quede con el tenant activo (nunca el que venga en el body).
        private void StampTenant()
        {
            var tenantId = CurrentTenantId;

            foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(e => e.State == EntityState.Added))
            {
                if (string.IsNullOrWhiteSpace(tenantId))
                    throw new InvalidOperationException("No hay un tenant activo: no se puede guardar información sin TenantId.");

                entry.Entity.TenantId = tenantId;
            }
        }
    }
}
