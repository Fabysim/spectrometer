namespace Spectrometre.Modules.ProfilEntreprise.Entities;

/// <summary>
/// Socle générique de suggestions de critères (contenu extrait en lecture seule de
/// <c>mvp/.../SeedDatabase.SeedManagerEntrepotCatalogAsync</c> — les 4 catégories transverses
/// uniquement, sans le catalogue métier « Compétences techniques » ni Manager Ventes / Marketing).
/// Texte d'aide à la saisie partagé par tous les tenants ; pas de table ni de seed EF.
/// </summary>
public static class CatalogueCriteresSuggeres
{
    public sealed record Item(string Categorie, string Libelle, string? Description);

    /// <summary>4 catégories, 20 items — socle générique mvp uniquement.</summary>
    public static IReadOnlyList<Item> Tous { get; } =
    [
        // Qualités personnelles
        new("Qualités personnelles", "Sens de l'engagement", "tenir ses promesses, respecter ses engagements"),
        new("Qualités personnelles", "Fiabilité et responsabilité", "assumer ses choix et leurs conséquences"),
        new("Qualités personnelles", "Résistance au stress", "gérer la pression et les imprévus"),
        new("Qualités personnelles", "Sens de l'éthique", "intégrité, respect des règles"),
        new("Qualités personnelles", "Empathie et diplomatie", null),
        new("Qualités personnelles", "Adaptabilité", null),

        // Aptitudes professionnelles
        new("Aptitudes professionnelles", "Organisation et planification", "atteindre des objectifs, structurer les missions"),
        new("Aptitudes professionnelles", "Leadership (motiver, fédérer)", null),
        new("Aptitudes professionnelles", "Prise de décision", "agir sans attendre, prendre des décisions rapides"),
        new("Aptitudes professionnelles", "Collaboration et travail en équipe", "travail en équipe, soutien mutuel"),
        new("Aptitudes professionnelles", "Négociation et gestion des conflits", "résolution de conflits, recherche de compromis"),
        new("Aptitudes professionnelles", "Sens de l'initiative", "agir sans attendre, prendre des décisions rapides"),

        // Attitudes socioprofessionnelles
        new("Attitudes socioprofessionnelles", "Attitude participative", "anticiper les besoins"),
        new("Attitudes socioprofessionnelles", "Attitude proactive", "agir avant les problèmes"),
        new("Attitudes socioprofessionnelles", "Attitude persévérante", "ne pas se décourager"),
        new("Attitudes socioprofessionnelles", "Attitude positive", "encourager l'équipe"),

        // Mode de fonctionnement naturel
        new("Mode de fonctionnement naturel", "Méthodique et organisé", "structurer le travail, planifier efficacement"),
        new("Mode de fonctionnement naturel", "Esprit critique et analytique", "analyser les situations objectivement"),
        new("Mode de fonctionnement naturel", "Décisionnel", "Prendre des décisions rapidement et assumer ses choix"),
        new("Mode de fonctionnement naturel", "Cohérence et constance", "Favoriser la cohérence et la constance dans ses actions"),
    ];
}
