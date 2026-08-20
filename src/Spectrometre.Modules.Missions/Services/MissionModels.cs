using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public sealed record ParticulierProfileView(
    int Id,
    string UserId,
    string Nom,
    string Prenoms,
    DateTimeOffset CreatedAt);

public sealed record PublierMissionInput(
    string Titre,
    string Description,
    string? Lieu,
    string? DureeEstimee,
    MissionDifficulte Difficulte,
    decimal? RemunerationMontant,
    string? CompetencesTravaillees);

public sealed record MissionResumeView(
    int MissionId,
    string Titre,
    string? Lieu,
    MissionStatut Statut,
    MissionDifficulte Difficulte,
    decimal? RemunerationMontant,
    DateTimeOffset CreatedAt);

public sealed record MissionDetailView(
    int MissionId,
    string Titre,
    string Description,
    string? Lieu,
    string? DureeEstimee,
    MissionDifficulte Difficulte,
    decimal? RemunerationMontant,
    string? CompetencesTravaillees,
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
    string? Lieu,
    MissionStatut MissionStatut,
    MissionAcceptationStatut AcceptationStatut,
    DateTimeOffset AccepteeLe,
    DateTimeOffset? DecideeLe);
