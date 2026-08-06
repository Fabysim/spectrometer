namespace Spectrometre.Modules.GestionDuTemps.Entities;

/// <summary>
/// Gabarit hebdomadaire récurrent d'une catégorie de temps pour un utilisateur — repris du concept
/// <c>GdtTypeDeTemps</c> de <c>mvp</c> (catégories par défaut : sommeil/perso/pro/admin/famille/social),
/// mais sans l'enveloppe <c>GdtCycle</c> (notion de saison/période, hors périmètre de ce cycle) : chaque
/// type de temps appartient directement à <see cref="UserId"/>, jamais à un cycle intermédiaire.
/// </summary>
public sealed class TypeDeTemps
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    /// <summary>Slug court (ex. "pro", "perso") — librement éditable par l'utilisateur, pas une énumération fermée (fidèle à mvp).</summary>
    public required string Cle { get; set; }

    public required string Libelle { get; set; }

    public TimeOnly HeureDebut { get; set; }

    public TimeOnly HeureFin { get; set; }

    /// <summary>Jours de récurrence, ex. "L,M,Me,J,V" — même convention que mvp.</summary>
    public string RecurrenceJours { get; set; } = "";

    public int OrdreAffichage { get; set; }

    /// <summary>
    /// Rattachement optionnel à une entreprise gérée par l'utilisateur (référence par identifiant vers
    /// <c>Spectrometre.Core.Tenancy.Company</c>, jamais une contrainte de clé étrangère inter-schéma — même
    /// principe que <c>CandidateProfileId</c> ailleurs dans la solution). Marquage informatif/filtrant
    /// uniquement : ce module reste à schéma fixe, ce n'est PAS une isolation multi-tenant. Vérifié contre
    /// <c>UserCompanyLink</c> à l'écriture par <c>GestionDuTempsService</c>.
    /// </summary>
    public int? CompanyId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
