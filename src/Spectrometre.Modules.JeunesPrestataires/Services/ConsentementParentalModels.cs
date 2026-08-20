namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>Modèle éditable du formulaire (hors champs d'identification jeune, dérivés du CV).</summary>
public sealed class ConsentementParentalFormModel
{
    public string? Parent1Nom { get; set; }
    public string? Parent1Lien { get; set; }
    public string? Parent1Adresse { get; set; }
    public string? Parent1Telephone { get; set; }
    public string? Parent1Email { get; set; }

    public string? Parent2Nom { get; set; }
    public string? Parent2Lien { get; set; }
    public string? Parent2Adresse { get; set; }
    public string? Parent2Telephone { get; set; }
    public string? Parent2Email { get; set; }

    public bool AutorisationMissions { get; set; }
    public bool AutorisationRevenus { get; set; }
    public decimal? PartParascolairePourcent { get; set; }
    public decimal? PartArgentDePochePourcent { get; set; }
    public string? AutreAffectation { get; set; }
    public string? ModalitesVersement { get; set; }
    public bool AutorisationDonneesEtImage { get; set; }

    public bool EngagementScolariteSanteEquilibre { get; set; }
    public bool EngagementInformerContraintes { get; set; }
    public bool EngagementEncouragerCharte { get; set; }
    public bool EngagementSignalerMissionInadaptee { get; set; }
    public bool EngagementCollaborerCoach { get; set; }

    public static ConsentementParentalFormModel FromEntity(Entities.ConsentementParental entity) => new()
    {
        Parent1Nom = entity.Parent1Nom,
        Parent1Lien = entity.Parent1Lien,
        Parent1Adresse = entity.Parent1Adresse,
        Parent1Telephone = entity.Parent1Telephone,
        Parent1Email = entity.Parent1Email,
        Parent2Nom = entity.Parent2Nom,
        Parent2Lien = entity.Parent2Lien,
        Parent2Adresse = entity.Parent2Adresse,
        Parent2Telephone = entity.Parent2Telephone,
        Parent2Email = entity.Parent2Email,
        AutorisationMissions = entity.AutorisationMissions,
        AutorisationRevenus = entity.AutorisationRevenus,
        PartParascolairePourcent = entity.PartParascolairePourcent,
        PartArgentDePochePourcent = entity.PartArgentDePochePourcent,
        AutreAffectation = entity.AutreAffectation,
        ModalitesVersement = entity.ModalitesVersement,
        AutorisationDonneesEtImage = entity.AutorisationDonneesEtImage,
        EngagementScolariteSanteEquilibre = entity.EngagementScolariteSanteEquilibre,
        EngagementInformerContraintes = entity.EngagementInformerContraintes,
        EngagementEncouragerCharte = entity.EngagementEncouragerCharte,
        EngagementSignalerMissionInadaptee = entity.EngagementSignalerMissionInadaptee,
        EngagementCollaborerCoach = entity.EngagementCollaborerCoach,
    };

    public void ApplyTo(Entities.ConsentementParental entity)
    {
        entity.Parent1Nom = Parent1Nom;
        entity.Parent1Lien = Parent1Lien;
        entity.Parent1Adresse = Parent1Adresse;
        entity.Parent1Telephone = Parent1Telephone;
        entity.Parent1Email = Parent1Email;
        entity.Parent2Nom = Parent2Nom;
        entity.Parent2Lien = Parent2Lien;
        entity.Parent2Adresse = Parent2Adresse;
        entity.Parent2Telephone = Parent2Telephone;
        entity.Parent2Email = Parent2Email;
        entity.AutorisationMissions = AutorisationMissions;
        entity.AutorisationRevenus = AutorisationRevenus;
        entity.PartParascolairePourcent = PartParascolairePourcent;
        entity.PartArgentDePochePourcent = PartArgentDePochePourcent;
        entity.AutreAffectation = AutreAffectation;
        entity.ModalitesVersement = ModalitesVersement;
        entity.AutorisationDonneesEtImage = AutorisationDonneesEtImage;
        entity.EngagementScolariteSanteEquilibre = EngagementScolariteSanteEquilibre;
        entity.EngagementInformerContraintes = EngagementInformerContraintes;
        entity.EngagementEncouragerCharte = EngagementEncouragerCharte;
        entity.EngagementSignalerMissionInadaptee = EngagementSignalerMissionInadaptee;
        entity.EngagementCollaborerCoach = EngagementCollaborerCoach;
    }
}

/// <summary>Vue complète pour l'écran (entité + métadonnées).</summary>
public sealed record ConsentementParentalView(
    Entities.ConsentementParental Entity,
    bool EstValide);

/// <summary>Résultat de <see cref="IConsentementParentalService.ConfirmerAsync"/>.</summary>
public sealed record ConsentementConfirmationResult(
    bool Success,
    IReadOnlyList<string> ChampsManquants);

/// <summary>Identifiants de champs manquants — clés de ressources <c>Champ_*</c>.</summary>
public static class ConsentementChamps
{
    public const string Parent1Nom = "Champ_Parent1Nom";
    public const string Parent1Lien = "Champ_Parent1Lien";
    public const string Parent1Adresse = "Champ_Parent1Adresse";
    public const string Parent1Telephone = "Champ_Parent1Telephone";
    public const string Parent1Email = "Champ_Parent1Email";
    public const string AutorisationMissions = "Champ_AutorisationMissions";
    public const string AutorisationRevenus = "Champ_AutorisationRevenus";
    public const string PartParascolairePourcent = "Champ_PartParascolairePourcent";
    public const string PartArgentDePochePourcent = "Champ_PartArgentDePochePourcent";
    public const string AutorisationDonneesEtImage = "Champ_AutorisationDonneesEtImage";
    public const string EngagementScolariteSanteEquilibre = "Champ_EngagementScolariteSanteEquilibre";
    public const string EngagementInformerContraintes = "Champ_EngagementInformerContraintes";
    public const string EngagementEncouragerCharte = "Champ_EngagementEncouragerCharte";
    public const string EngagementSignalerMissionInadaptee = "Champ_EngagementSignalerMissionInadaptee";
    public const string EngagementCollaborerCoach = "Champ_EngagementCollaborerCoach";
    public const string NomJeuneConfirmation = "Champ_NomJeuneConfirmation";
    public const string NomParent1Confirmation = "Champ_NomParent1Confirmation";
    public const string NomParent2Confirmation = "Champ_NomParent2Confirmation";
}
