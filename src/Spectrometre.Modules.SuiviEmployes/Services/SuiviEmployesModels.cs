using Spectrometre.Modules.SuiviEmployes.Entities;

namespace Spectrometre.Modules.SuiviEmployes.Services;

public sealed record PointCourbe(string Label, double Valeur);

public sealed record SerieCritereCourbe(
    int CritereId,
    string Categorie,
    string Libelle,
    IReadOnlyList<PointCourbe> Actuel,
    IReadOnlyList<PointCourbe> Souhaite);

public sealed record EmployeContexte(
    int UserCompanyLinkId,
    string UserId,
    string Email,
    int CompanyId,
    string CompanyName,
    string SchemaName,
    int? PosteId,
    string? PosteTitre,
    bool SeuilCritiqueAtteint);

public sealed record EmployeRattachementOption(
    int UserCompanyLinkId,
    int CompanyId,
    string CompanyName,
    string? PosteTitre);

public sealed record CritereProfilView(
    int CritereId,
    string Categorie,
    string Libelle);

public sealed record ScoreBlocView(
    int CritereId,
    int ScoreActuel,
    int ScoreSouhaite);

public sealed record BlocEvaluationView(
    DateOnly EvaluationDate,
    int DaySequence,
    bool IsClosed,
    IReadOnlyList<ScoreBlocView> Scores);

public sealed record ProfilProfessionnelPageData(
    EmployeContexte Contexte,
    bool ValidationInitialeFaite,
    IReadOnlyList<CritereProfilView> Criteres,
    IReadOnlyList<BlocEvaluationView> Blocs);

public sealed record ScoreSaisieDto(int CritereId, int ScoreActuel, int ScoreSouhaite);

public sealed record ObjectifView(
    int Id,
    DateOnly Date,
    string Titre,
    string? Moyens,
    AtteinteObjectif Atteinte,
    string? Observation,
    int? Note);

public sealed record EvaluationObjectifsView(
    int Id,
    DateOnly DateDebut,
    DateOnly DateFin,
    bool Archivee,
    bool SeuilCritiqueAtteint,
    IReadOnlyList<ObjectifView> Objectifs);

public sealed record ObjectifSaisieDto(
    int? Id,
    DateOnly Date,
    string Titre,
    string? Moyens,
    AtteinteObjectif Atteinte,
    string? Observation,
    int? Note);

public sealed record AnalyseIaEmployeView(
    string AnalyseMarkdown,
    DateTimeOffset GenereeLe,
    bool GenereeParIa,
    string? Avertissement = null);
