namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Consentement parental pour un jeune prestataire mineur — un enregistrement par candidat.
/// L'identification du jeune (section 1 du document source) est dérivée de
/// <see cref="JeuneProfile"/>, jamais dupliquée ici.
/// </summary>
public sealed class ConsentementParental
{
    public int Id { get; set; }
    public int JeuneProfileId { get; set; }

    // Représentant légal 1 (obligatoire)
    public string? Parent1Nom { get; set; }
    public string? Parent1Lien { get; set; }
    public string? Parent1Adresse { get; set; }
    public string? Parent1Telephone { get; set; }
    public string? Parent1Email { get; set; }

    // Représentant légal 2 (facultatif)
    public string? Parent2Nom { get; set; }
    public string? Parent2Lien { get; set; }
    public string? Parent2Adresse { get; set; }
    public string? Parent2Telephone { get; set; }
    public string? Parent2Email { get; set; }

    // Autorisations
    public bool AutorisationMissions { get; set; }
    public bool AutorisationRevenus { get; set; }
    public decimal? PartParascolairePourcent { get; set; }
    public decimal? PartArgentDePochePourcent { get; set; }
    public string? AutreAffectation { get; set; }
    public string? ModalitesVersement { get; set; }
    public bool AutorisationDonneesEtImage { get; set; }

    // Engagements des parents (section 6 du document source)
    public bool EngagementScolariteSanteEquilibre { get; set; }
    public bool EngagementInformerContraintes { get; set; }
    public bool EngagementEncouragerCharte { get; set; }
    public bool EngagementSignalerMissionInadaptee { get; set; }
    public bool EngagementCollaborerCoach { get; set; }

    /// <summary>
    /// Confirmation finale : noms tapés (pas de signature électronique cryptographique).
    /// </summary>
    public string? NomJeuneConfirmation { get; set; }
    public string? NomParent1Confirmation { get; set; }
    public string? NomParent2Confirmation { get; set; }

    /// <summary>Null tant que le consentement n'est pas finalisé.</summary>
    public DateTimeOffset? ValideLe { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
