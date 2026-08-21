using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public sealed record ParticulierProfileView(
    int Id,
    string UserId,
    string Nom,
    string Prenoms,
    DateTimeOffset CreatedAt);

public sealed record PublierMissionInput(
    string? Titre,
    string Description,
    string? Lieu,
    string? DureeEstimee,
    MissionDifficulte Difficulte,
    decimal? RemunerationMontant,
    string? CompetencesTravaillees,
    MissionCategorie Categorie,
    MissionNiveauEncadrement NiveauEncadrement,
    bool PresenceEscaliers = false,
    bool PresenceAnimaux = false,
    bool PortDeCharge = false,
    bool AccesDifficile = false,
    string? RisqueParticulier = null);

public sealed record MissionResumeView(
    int MissionId,
    string Titre,
    MissionCategorie Categorie,
    string? Lieu,
    MissionStatut Statut,
    MissionDifficulte Difficulte,
    MissionNiveauEncadrement NiveauEncadrement,
    decimal? RemunerationMontant,
    bool PresenceEscaliers,
    bool PresenceAnimaux,
    bool PortDeCharge,
    bool AccesDifficile,
    string? RisqueParticulier,
    DateTimeOffset CreatedAt,
    /// <summary>Acceptation validée liée (pour évaluation particulier si mission terminée).</summary>
    int? AcceptationIdPourEvaluation = null,
    /// <summary>Prénom uniquement, si mission <c>Attribuee</c> ou <c>Terminee</c>.</summary>
    string? JeunePrenom = null,
    /// <summary>Motif de refus de publication, si la mission a été refusée en modération.</summary>
    string? MotifAnnulation = null);

public sealed record MissionDetailView(
    int MissionId,
    string Titre,
    MissionCategorie Categorie,
    string Description,
    string? Lieu,
    string? DureeEstimee,
    MissionDifficulte Difficulte,
    MissionNiveauEncadrement NiveauEncadrement,
    decimal? RemunerationMontant,
    string? CompetencesTravaillees,
    bool PresenceEscaliers,
    bool PresenceAnimaux,
    bool PortDeCharge,
    bool AccesDifficile,
    string? RisqueParticulier,
    MissionStatut Statut,
    DateTimeOffset CreatedAt);

public sealed record MissionAcceptationView(
    int AcceptationId,
    int MissionId,
    string MissionTitre,
    int JeuneProfileId,
    string JeuneNom,
    string JeunePrenoms,
    MissionAcceptationStatut Statut,
    MissionStatut MissionStatut,
    DateTimeOffset AccepteeLe,
    DateTimeOffset? DecideeLe);

public sealed record MissionJeuneView(
    int AcceptationId,
    int MissionId,
    string Titre,
    MissionCategorie Categorie,
    string? Lieu,
    MissionStatut MissionStatut,
    MissionAcceptationStatut AcceptationStatut,
    MissionNiveauEncadrement NiveauEncadrement,
    bool PresenceEscaliers,
    bool PresenceAnimaux,
    bool PortDeCharge,
    bool AccesDifficile,
    string? RisqueParticulier,
    DateTimeOffset AccepteeLe,
    DateTimeOffset? DecideeLe);

/// <summary>État mutable du formulaire publier / modifier (évite de dupliquer les champs Razor).</summary>
public sealed class MissionFormModel
{
    public MissionCategorie Categorie { get; set; } = MissionCategorie.JardinageSimple;
    public string Titre { get; set; } = "";
    public string Description { get; set; } = "";
    public MissionNiveauEncadrement NiveauEncadrement { get; set; } = MissionNiveauEncadrement.PresentPendantMission;
    public string? Lieu { get; set; }
    public string? DureeEstimee { get; set; }
    public MissionDifficulte Difficulte { get; set; } = MissionDifficulte.Facile;
    public decimal? RemunerationMontant { get; set; }
    public string? CompetencesTravaillees { get; set; }
    public bool PresenceEscaliers { get; set; }
    public bool PresenceAnimaux { get; set; }
    public bool PortDeCharge { get; set; }
    public bool AccesDifficile { get; set; }
    public string? RisqueParticulier { get; set; }

    public PublierMissionInput ToInput() => new(
        Titre,
        Description,
        Lieu,
        DureeEstimee,
        Difficulte,
        RemunerationMontant,
        CompetencesTravaillees,
        Categorie,
        NiveauEncadrement,
        PresenceEscaliers,
        PresenceAnimaux,
        PortDeCharge,
        AccesDifficile,
        RisqueParticulier);

    public static MissionFormModel FromDetail(MissionDetailView d) => new()
    {
        Categorie = d.Categorie,
        Titre = d.Titre,
        Description = d.Description,
        NiveauEncadrement = d.NiveauEncadrement,
        Lieu = d.Lieu,
        DureeEstimee = d.DureeEstimee,
        Difficulte = d.Difficulte,
        RemunerationMontant = d.RemunerationMontant,
        CompetencesTravaillees = d.CompetencesTravaillees,
        PresenceEscaliers = d.PresenceEscaliers,
        PresenceAnimaux = d.PresenceAnimaux,
        PortDeCharge = d.PortDeCharge,
        AccesDifficile = d.AccesDifficile,
        RisqueParticulier = d.RisqueParticulier,
    };
}
