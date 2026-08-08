namespace Spectrometre.Modules.ProfilEntreprise.Entities;

public enum PosteStatut
{
    Ouvert = 0,
    Ferme = 1,
}

/// <summary>Libellés d'affichage du statut (bilinguisme, cycle contenu métier) — le statut lui-même reste un enum stocké tel quel, ces libellés ne sont utilisés qu'à l'affichage.</summary>
public static class PosteStatutLabels
{
    public static string Label(PosteStatut statut, bool english) => statut switch
    {
        PosteStatut.Ouvert => english ? "Open" : "Ouvert",
        PosteStatut.Ferme => english ? "Closed" : "Fermé",
        _ => statut.ToString(),
    };
}

/// <summary>
/// Un poste à pourvoir, rattaché à l'entreprise active (schéma tenant — voir <c>ITenantScopedDbContext</c>,
/// même principe que Profil Entreprise). La grille d'exigences manuelle vit dans
/// <see cref="CritereEvaluation"/> (catégorie / libellé / niveau requis).
/// </summary>
public sealed class Poste
{
    public int Id { get; set; }
    public required string Titre { get; set; }
    public string? Description { get; set; }

    /// <summary>Texte libre pour ce cycle — un référentiel de départements viendra avec un module RH plus complet.</summary>
    public string? Departement { get; set; }

    /// <summary>Description des tâches (équivalent mvp <c>TasksDescription</c>).</summary>
    public string? TachesDescription { get; set; }

    /// <summary>Salaire en texte libre (équivalent mvp — pas un montant numérique).</summary>
    public string? Salaire { get; set; }

    /// <summary>Avantages sociaux (équivalent mvp <c>Benefits</c>).</summary>
    public string? Avantages { get; set; }

    /// <summary>Date/heure de clôture en UTC (équivalent mvp <c>ClosingDate</c>).</summary>
    public DateTimeOffset? DateCloture { get; set; }

    public PosteStatut Statut { get; set; } = PosteStatut.Ouvert;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Texte d'offre d'emploi généré (IA ou repli local) — écrasé à chaque régénération.</summary>
    public string? OffreTexte { get; set; }

    public DateTimeOffset? OffreGenereeLe { get; set; }

    /// <summary><c>true</c> si le dernier texte vient de Claude ; <c>false</c> pour le repli déterministe.</summary>
    public bool OffreGenereeParIa { get; set; }
}
