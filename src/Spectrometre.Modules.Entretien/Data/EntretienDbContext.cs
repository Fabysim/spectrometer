using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Entities;
using Spectrometre.Modules.Entretien.Entities;

namespace Spectrometre.Modules.Entretien.Data;

/// <summary>
/// Schéma = celui de l'entreprise active. <see cref="TenantSchema"/> affecté par l'appelant après création
/// via <c>IDbContextFactory</c> — voir le commentaire détaillé sur <c>ProfilEntrepriseDbContext</c>
/// (même pattern que tous les autres DbContext tenant-scopés de la solution).
/// </summary>
public sealed class EntretienDbContext(DbContextOptions<EntretienDbContext> options)
    : DbContext(options), ITenantScopedDbContext
{
    public string TenantSchema { get; set; } = "public";

    public DbSet<QuestionTemplate> QuestionTemplates => Set<QuestionTemplate>();
    public DbSet<InterviewGenerationSettings> InterviewGenerationSettings => Set<InterviewGenerationSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<InterviewGenerationSettings>(e =>
        {
            e.HasData(new InterviewGenerationSettings { Id = 1, SeuilAxeFaiblePercent = 60 });
        });

        builder.Entity<QuestionTemplate>(e =>
        {
            e.HasIndex(q => new { q.Type, q.Axis, q.Sens, q.DisplayOrder });
            e.HasData(SeedQuestionTemplates());
        });
    }

    /// <summary>
    /// Gabarits de départ, un exemple concret par axe et par sens (voir le document de cadrage). Modifiable
    /// directement dans cette table en base — <c>{axe}</c>/<c>{score}</c> disponibles partout,
    /// <c>{rythmeCandidat}</c>/<c>{rythmeEntreprise}</c> uniquement pour l'axe organisationnel, <c>{tag}</c>
    /// uniquement pour les gabarits de type <see cref="QuestionTemplateType.PointVigilance"/>.
    /// </summary>
    private static List<QuestionTemplate> SeedQuestionTemplates()
    {
        var id = 0;
        QuestionTemplate T(QuestionTemplateType type, CompatibilityAxis? axis, QuestionSens sens, string gabarit, int ordre = 0) =>
            new() { Id = ++id, Type = type, Axis = axis, Sens = sens, Gabarit = gabarit, DisplayOrder = ordre };

        return
        [
            // --- Technique ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Technique, QuestionSens.EntrepriseVersCandidat,
                "Quelles sont, selon vous, les compétences techniques qui vous manquent encore pour être pleinement opérationnel sur ce poste ?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Technique, QuestionSens.CandidatVersEntreprise,
                "Quel accompagnement ou quelle formation l'entreprise prévoit-elle pour combler un éventuel écart de compétences techniques ?"),

            // --- Comportementale ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Comportementale, QuestionSens.EntrepriseVersCandidat,
                "Pouvez-vous décrire une situation récente où votre façon de travailler au quotidien a été mise à l'épreuve ?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Comportementale, QuestionSens.CandidatVersEntreprise,
                "Quels comportements professionnels l'entreprise valorise-t-elle le plus au quotidien, au-delà de ce qui est affiché ?"),

            // --- Culturelle ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Culturelle, QuestionSens.EntrepriseVersCandidat,
                "Qu'est-ce qui, dans la culture d'une entreprise, vous a déjà mis mal à l'aise par le passé ?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Culturelle, QuestionSens.CandidatVersEntreprise,
                "Comment la culture d'entreprise affichée se traduit-elle concrètement dans les décisions du quotidien ?"),

            // --- Organisationnelle (rythme) : seul axe utilisant {rythmeCandidat}/{rythmeEntreprise} ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Organisationnelle, QuestionSens.EntrepriseVersCandidat,
                "Vous avez indiqué tolérer un rythme « {rythmeCandidat} », alors que ce poste est annoncé avec un rythme « {rythmeEntreprise} » — comment envisagez-vous cet écart au quotidien ?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Organisationnelle, QuestionSens.CandidatVersEntreprise,
                "Le poste est annoncé avec un rythme « {rythmeEntreprise} » — à quoi ressemble concrètement une semaine type pour quelqu'un qui tolère plutôt un rythme « {rythmeCandidat} » ?"),

            // --- Motivationnelle ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Motivationnelle, QuestionSens.EntrepriseVersCandidat,
                "Qu'est-ce qui vous ferait perdre votre motivation le plus rapidement dans ce poste ?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Motivationnelle, QuestionSens.CandidatVersEntreprise,
                "Qu'est-ce que l'entreprise met concrètement en place pour nourrir la motivation de ses équipes ?"),

            // --- Points de vigilance partagés : génériques, paramétrés par {tag} ---
            T(QuestionTemplateType.PointVigilance, null, QuestionSens.EntrepriseVersCandidat,
                "Vous avez signalé « {tag} » comme un point de vigilance potentiel — pouvez-vous préciser ce que cela représente concrètement pour vous, et comment vous l'avez géré par le passé ?"),
            T(QuestionTemplateType.PointVigilance, null, QuestionSens.CandidatVersEntreprise,
                "L'entreprise a également identifié « {tag} » comme un point de vigilance — comment cela se manifeste-t-il concrètement au quotidien dans l'équipe ?"),
        ];
    }
}
