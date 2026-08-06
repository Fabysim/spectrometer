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

    // ── Formulaire de CV (sections 1 à 8 du document source) ────────────────
    public DbSet<CvCoordonnees> CvCoordonnees => Set<CvCoordonnees>();
    public DbSet<CvFormation> CvFormations => Set<CvFormation>();
    public DbSet<CvCompetencesEtudes> CvCompetencesEtudes => Set<CvCompetencesEtudes>();
    public DbSet<CvExperience> CvExperiences => Set<CvExperience>();
    public DbSet<CvCaracteristiquesPersonnelles> CvCaracteristiquesPersonnelles => Set<CvCaracteristiquesPersonnelles>();
    public DbSet<CvLoisirs> CvLoisirs => Set<CvLoisirs>();
    public DbSet<CvReference> CvReferences => Set<CvReference>();
    public DbSet<CvDeclaration> CvDeclarations => Set<CvDeclaration>();

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
            // Même jeton de concurrence optimiste que CandidateCompatibilityCriteria (voir son commentaire) —
            // une réponse texte perdue par écriture concurrente serait le même bug de perte de mise à jour,
            // pas juste un oubli cosmétique. Voir CandidateProfileService.MutateAnswerAsync.
            e.Property<uint>("Version").IsRowVersion();
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

        ConfigureCv(builder);
        SeedQuestionnaire(builder);
    }

    /// <summary>
    /// Sections du formulaire de CV — même discipline que <see cref="CandidateCompatibilityCriteria"/> :
    /// index unique sur <c>CandidateProfileId</c> pour les sections 1:1, jeton de concurrence optimiste
    /// xmin sur toutes (même en contention attendue faible, un seul candidat éditant son propre CV — voir
    /// <c>CandidateProfileService</c>, pour éviter de réintroduire la classe de bug de perte de mise à jour
    /// déjà rencontrée deux fois sur ce module).
    /// </summary>
    private static void ConfigureCv(ModelBuilder builder)
    {
        builder.Entity<CvCoordonnees>(e =>
        {
            e.HasIndex(c => c.CandidateProfileId).IsUnique();
            e.Property<uint>("Version").IsRowVersion();
        });

        builder.Entity<CvFormation>(e =>
        {
            e.HasIndex(f => f.CandidateProfileId);
            e.Property<uint>("Version").IsRowVersion();
        });

        builder.Entity<CvCompetencesEtudes>(e =>
        {
            e.HasIndex(c => c.CandidateProfileId).IsUnique();
            e.Property<uint>("Version").IsRowVersion();
        });

        builder.Entity<CvExperience>(e =>
        {
            e.HasIndex(x => x.CandidateProfileId);
            e.Property<uint>("Version").IsRowVersion();
        });

        builder.Entity<CvCaracteristiquesPersonnelles>(e =>
        {
            e.HasIndex(c => c.CandidateProfileId).IsUnique();
            e.Property<uint>("Version").IsRowVersion();
        });

        builder.Entity<CvLoisirs>(e =>
        {
            e.HasIndex(l => l.CandidateProfileId).IsUnique();
            e.Property<uint>("Version").IsRowVersion();
        });

        builder.Entity<CvReference>(e =>
        {
            e.HasIndex(r => r.CandidateProfileId);
            e.Property<uint>("Version").IsRowVersion();
        });

        builder.Entity<CvDeclaration>(e =>
        {
            e.HasIndex(d => d.CandidateProfileId).IsUnique();
            e.Property<uint>("Version").IsRowVersion();
        });
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
