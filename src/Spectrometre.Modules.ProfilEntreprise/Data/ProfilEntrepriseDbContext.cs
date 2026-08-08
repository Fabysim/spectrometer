using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilEntreprise.Entities;

namespace Spectrometre.Modules.ProfilEntreprise.Data;

/// <summary>
/// Schéma = celui de l'entreprise active, sur le principe « une entreprise = un schéma » de V1.
/// <see cref="TenantSchema"/> n'est PAS résolu via <see cref="ITenantContext"/> injecté au constructeur :
/// ce DbContext est créé via <c>IDbContextFactory</c> (voir <c>AddDbContextFactory</c> dans
/// ServiceCollectionExtensions), dont les instances sont construites depuis le root service provider,
/// qui ne peut pas résoudre un service scoped comme ITenantContext. C'est donc l'appelant (scoped, qui a
/// accès à ITenantContext) qui affecte <see cref="TenantSchema"/> juste après <c>CreateDbContextAsync()</c>,
/// avant toute requête (le modèle EF n'est construit qu'au premier accès).
/// </summary>
public sealed class ProfilEntrepriseDbContext(DbContextOptions<ProfilEntrepriseDbContext> options)
    : DbContext(options), ITenantScopedDbContext
{
    public string TenantSchema { get; set; } = "public";

    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<CompanyQuestion> CompanyQuestions => Set<CompanyQuestion>();
    public DbSet<CompanyAnswer> CompanyAnswers => Set<CompanyAnswer>();
    public DbSet<CompanyCompatibilityCriteria> CompanyCompatibilityCriteria => Set<CompanyCompatibilityCriteria>();

    public DbSet<Poste> Postes => Set<Poste>();
    public DbSet<Candidature> Candidatures => Set<Candidature>();
    public DbSet<CritereEvaluation> CriteresEvaluation => Set<CritereEvaluation>();
    public DbSet<EvaluationCritereCandidature> EvaluationsCriteresCandidature => Set<EvaluationCritereCandidature>();
    public DbSet<GenerationCriteresIaPoste> GenerationsCriteresIaPoste => Set<GenerationCriteresIaPoste>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<CompanyProfile>(e =>
        {
            // Un schéma tenant = une entreprise = un seul profil (contrairement à CandidateProfile/CoachProfile,
            // qui ont un index unique applicatif sur UserId — ici il n'y a pas de colonne applicative naturelle
            // à contraindre, le schéma représente déjà l'entreprise). Colonne fantôme fixée à 1 + index unique :
            // garantit qu'une seule ligne peut exister dans ce schéma, quelle que soit sa valeur d'Id. Voir
            // CompanyProfileService.GetOrCreateProfileIdAsync pour le correctif de la course qui, avant cette
            // contrainte, pouvait créer plusieurs lignes silencieusement (aucune exception ne le signalait).
            e.Property<int>("Singleton").HasDefaultValue(1);
            e.HasIndex("Singleton").IsUnique();
        });

        builder.Entity<CompanyQuestion>(e => e.HasIndex(q => q.Number).IsUnique());
        builder.Entity<CompanyAnswer>(e => e.HasIndex(a => new { a.CompanyProfileId, a.QuestionId }).IsUnique());
        builder.Entity<CompanyCompatibilityCriteria>(e =>
        {
            e.HasIndex(c => c.CompanyProfileId).IsUnique();
            // Voir le commentaire équivalent côté ProfilCandidatDbContext : jeton de concurrence optimiste
            // (uint fantôme + IsRowVersion, auto-détecté par le provider Npgsql et mappé sur la colonne
            // système "xmin"), utilisé par CompanyProfileService pour détecter puis résoudre les écritures concurrentes.
            e.Property<uint>("Version").IsRowVersion();
        });

        builder.Entity<Candidature>(e =>
        {
            e.HasIndex(c => new { c.PosteId, c.CandidateProfileId }).IsUnique();
            e.HasMany(c => c.EvaluationsFinales)
                .WithOne()
                .HasForeignKey(ev => ev.CandidatureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CritereEvaluation>(e =>
        {
            e.HasIndex(c => new { c.PosteId, c.OrdreAffichage });
            e.Property(c => c.Categorie).HasMaxLength(200);
            e.Property(c => c.Libelle).HasMaxLength(500);
        });

        builder.Entity<EvaluationCritereCandidature>(e =>
        {
            e.HasIndex(ev => new { ev.CandidatureId, ev.CritereId }).IsUnique();
            e.HasOne<CritereEvaluation>()
                .WithMany()
                .HasForeignKey(ev => ev.CritereId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GenerationCriteresIaPoste>(e =>
        {
            e.HasIndex(g => g.PosteId).IsUnique();
            e.Property(g => g.HashContexte).HasMaxLength(128);
            e.HasOne<Poste>()
                .WithMany()
                .HasForeignKey(g => g.PosteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedQuestionnaire(builder);
    }

    private static void SeedQuestionnaire(ModelBuilder builder)
    {
        var questions = CompanyQuestionnaireSeed.Questions.Select(seed => new CompanyQuestion
        {
            Id = seed.Number,
            Number = seed.Number,
            Theme = seed.Theme,
            Text = seed.Text,
            TextEn = seed.TextEn,
        });

        builder.Entity<CompanyQuestion>().HasData(questions);
    }
}
