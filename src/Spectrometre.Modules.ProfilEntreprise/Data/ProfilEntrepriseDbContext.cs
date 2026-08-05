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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<CompanyQuestion>(e => e.HasIndex(q => q.Number).IsUnique());
        builder.Entity<CompanyAnswer>(e => e.HasIndex(a => new { a.CompanyProfileId, a.QuestionId }).IsUnique());
        builder.Entity<CompanyCompatibilityCriteria>(e => e.HasIndex(c => c.CompanyProfileId).IsUnique());

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
        });

        builder.Entity<CompanyQuestion>().HasData(questions);
    }
}
