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
        QuestionTemplate T(QuestionTemplateType type, CompatibilityAxis? axis, QuestionSens sens, string gabarit, string gabaritEn, int ordre = 0) =>
            new() { Id = ++id, Type = type, Axis = axis, Sens = sens, Gabarit = gabarit, GabaritEn = gabaritEn, DisplayOrder = ordre };

        return
        [
            // --- Technique ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Technique, QuestionSens.EntrepriseVersCandidat,
                "Quelles sont, selon vous, les compétences techniques qui vous manquent encore pour être pleinement opérationnel sur ce poste ?",
                "In your view, which technical skills are you still missing to be fully operational in this role?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Technique, QuestionSens.CandidatVersEntreprise,
                "Quel accompagnement ou quelle formation l'entreprise prévoit-elle pour combler un éventuel écart de compétences techniques ?",
                "What support or training does the company plan to offer to close any technical skills gap?"),

            // --- Comportementale ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Comportementale, QuestionSens.EntrepriseVersCandidat,
                "Pouvez-vous décrire une situation récente où votre façon de travailler au quotidien a été mise à l'épreuve ?",
                "Can you describe a recent situation where your day-to-day way of working was put to the test?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Comportementale, QuestionSens.CandidatVersEntreprise,
                "Quels comportements professionnels l'entreprise valorise-t-elle le plus au quotidien, au-delà de ce qui est affiché ?",
                "Which professional behaviors does the company value most day-to-day, beyond what's officially stated?"),

            // --- Culturelle ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Culturelle, QuestionSens.EntrepriseVersCandidat,
                "Qu'est-ce qui, dans la culture d'une entreprise, vous a déjà mis mal à l'aise par le passé ?",
                "What, in a company's culture, has made you uncomfortable in the past?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Culturelle, QuestionSens.CandidatVersEntreprise,
                "Comment la culture d'entreprise affichée se traduit-elle concrètement dans les décisions du quotidien ?",
                "How does the company's stated culture concretely translate into day-to-day decisions?"),

            // --- Organisationnelle (rythme) : seul axe utilisant {rythmeCandidat}/{rythmeEntreprise} ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Organisationnelle, QuestionSens.EntrepriseVersCandidat,
                "Vous avez indiqué tolérer un rythme « {rythmeCandidat} », alors que ce poste est annoncé avec un rythme « {rythmeEntreprise} » — comment envisagez-vous cet écart au quotidien ?",
                "You indicated you can tolerate a \"{rythmeCandidat}\" pace, while this role is advertised with a \"{rythmeEntreprise}\" pace — how do you see yourself handling this gap day-to-day?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Organisationnelle, QuestionSens.CandidatVersEntreprise,
                "Le poste est annoncé avec un rythme « {rythmeEntreprise} » — à quoi ressemble concrètement une semaine type pour quelqu'un qui tolère plutôt un rythme « {rythmeCandidat} » ?",
                "This role is advertised with a \"{rythmeEntreprise}\" pace — what does a typical week concretely look like for someone who instead tolerates a \"{rythmeCandidat}\" pace?"),

            // --- Motivationnelle ---
            T(QuestionTemplateType.Axe, CompatibilityAxis.Motivationnelle, QuestionSens.EntrepriseVersCandidat,
                "Qu'est-ce qui vous ferait perdre votre motivation le plus rapidement dans ce poste ?",
                "What would make you lose motivation fastest in this role?"),
            T(QuestionTemplateType.Axe, CompatibilityAxis.Motivationnelle, QuestionSens.CandidatVersEntreprise,
                "Qu'est-ce que l'entreprise met concrètement en place pour nourrir la motivation de ses équipes ?",
                "What does the company concretely put in place to nurture its teams' motivation?"),

            // --- Points de vigilance partagés : génériques, paramétrés par {tag} ---
            T(QuestionTemplateType.PointVigilance, null, QuestionSens.EntrepriseVersCandidat,
                "Vous avez signalé « {tag} » comme un point de vigilance potentiel — pouvez-vous préciser ce que cela représente concrètement pour vous, et comment vous l'avez géré par le passé ?",
                "You flagged \"{tag}\" as a potential point of caution — can you clarify what this concretely means to you, and how you've handled it in the past?"),
            T(QuestionTemplateType.PointVigilance, null, QuestionSens.CandidatVersEntreprise,
                "L'entreprise a également identifié « {tag} » comme un point de vigilance — comment cela se manifeste-t-il concrètement au quotidien dans l'équipe ?",
                "The company also identified \"{tag}\" as a point of caution — how does this concretely show up day-to-day within the team?"),
        ];
    }
}
