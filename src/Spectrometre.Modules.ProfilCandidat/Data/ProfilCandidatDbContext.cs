using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Data;

/// <summary>
/// Schéma fixe <c>profil_candidat</c> — non tenant-scopé : un candidat n'est pas une entreprise,
/// son profil doit rester accessible quelle que soit l'entreprise active au moment de la requête.
/// </summary>
public sealed class ProfilCandidatDbContext(DbContextOptions<ProfilCandidatDbContext> options) : DbContext(options)
{
    public const string SchemaName = "profil_candidat";

    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<CandidateQuestion> CandidateQuestions => Set<CandidateQuestion>();
    public DbSet<CandidateQuestionExample> CandidateQuestionExamples => Set<CandidateQuestionExample>();
    public DbSet<CandidateAnswer> CandidateAnswers => Set<CandidateAnswer>();
    public DbSet<CandidateSynthesisTag> CandidateSynthesisTags => Set<CandidateSynthesisTag>();
    public DbSet<CandidateCompatibilityCriteria> CandidateCompatibilityCriteria => Set<CandidateCompatibilityCriteria>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<CandidateProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
        });

        builder.Entity<CandidateQuestion>(e =>
        {
            e.HasIndex(q => q.Number).IsUnique();
            e.HasMany(q => q.Examples)
                .WithOne()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CandidateAnswer>(e =>
        {
            e.HasIndex(a => new { a.CandidateProfileId, a.QuestionId }).IsUnique();
        });

        builder.Entity<CandidateCompatibilityCriteria>(e =>
        {
            e.HasIndex(c => c.CandidateProfileId).IsUnique();
            // Jeton de concurrence optimiste : propriété fantôme uint + IsRowVersion() — le provider
            // Npgsql détecte automatiquement ce motif (propriété uint, ValueGeneratedOnAddOrUpdate,
            // ConcurrencyToken) et la fait pointer vers la colonne système Postgres "xmin" (déjà présente
            // sur toute table, incrémentée à chaque UPDATE), sans générer de migration pour une colonne
            // qui existe déjà. Voir CandidateProfileService.MutateCriteriaAsync pour le correctif du
            // problème de perte de mise à jour sur cette entité (deux sauvegardes concurrentes de la grille H).
            e.Property<uint>("Version").IsRowVersion();
        });

        SeedQuestionnaire(builder);
    }

    /// <summary>Seed statique du catalogue de questions — contenu figé issu du document source, pas une donnée éditable par l'utilisateur.</summary>
    private static void SeedQuestionnaire(ModelBuilder builder)
    {
        var questions = new List<CandidateQuestion>();
        var examples = new List<CandidateQuestionExample>();
        var exampleId = 1;

        foreach (var seed in CandidateQuestionnaireSeed.Questions)
        {
            questions.Add(new CandidateQuestion
            {
                Id = seed.Number,
                Number = seed.Number,
                Theme = seed.Theme,
                Text = seed.Text,
            });

            for (var i = 0; i < seed.Examples.Length; i++)
            {
                examples.Add(new CandidateQuestionExample
                {
                    Id = exampleId++,
                    QuestionId = seed.Number,
                    Text = seed.Examples[i],
                    DisplayOrder = i,
                });
            }
        }

        builder.Entity<CandidateQuestion>().HasData(questions);
        builder.Entity<CandidateQuestionExample>().HasData(examples);
    }
}
