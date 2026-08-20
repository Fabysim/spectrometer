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
    int? AcceptationIdPourEvaluation = null);

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
