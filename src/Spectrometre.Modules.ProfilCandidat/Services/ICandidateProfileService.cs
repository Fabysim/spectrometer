using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>Vue exposée publiquement d'une question avec la réponse courante du candidat, si elle existe.</summary>
public sealed record CandidateQuestionView(int QuestionId, CandidateTheme Theme, int Number, string Text, IReadOnlyList<string> Examples, string? AnswerText, DateTimeOffset? UpdatedAt);

public sealed record CandidateSynthesisView(IReadOnlyDictionary<SynthesisCategory, IReadOnlyList<string>> TagsByCategory, DateTimeOffset GeneratedAt);

/// <summary>
/// Critères structurés (tags du vocabulaire partagé + échelle) utilisés pour le scoring, accompagnés de
/// notes libres optionnelles pour la nuance humaine — les <c>*Notes</c> ne sont jamais utilisées dans
/// le calcul de compatibilité, uniquement affichées en contexte.
/// </summary>
public sealed record CandidateCompatibilityCriteriaView(
    IReadOnlyList<string> TechniqueTags,
    IReadOnlyList<string> ComportementaleTags,
    IReadOnlyList<string> CulturelleTags,
    int? RythmeTravail,
    IReadOnlyList<string> MotivationnelleTags,
    IReadOnlyList<string> PointsVigilanceTags,
    string? TechniqueNotes,
    string? ComportementaleNotes,
    string? CulturelleNotes,
    string? OrganisationnelleNotes,
    string? MotivationnelleNotes,
    string? PointsVigilanceNotes);

/// <summary>
/// Point d'entrée public du module Profil Candidat. Le module Compatibilité passe exclusivement par
/// cette interface — jamais d'accès direct à <c>ProfilCandidatDbContext</c> depuis l'extérieur du module.
/// </summary>
public interface ICandidateProfileService
{
    Task<int> GetOrCreateProfileIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Les 6 thèmes du questionnaire avec, pour chaque question, la réponse déjà donnée par ce candidat le cas échéant.</summary>
    Task<IReadOnlyList<CandidateQuestionView>> GetQuestionnaireAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    Task SaveAnswerAsync(int candidateProfileId, int questionId, string? answerText, CancellationToken cancellationToken = default);

    /// <summary>Régénère la synthèse de profil à partir des réponses actuelles (heuristique simple, voir implémentation).</summary>
    Task<CandidateSynthesisView> GenerateSynthesisAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    Task<CandidateSynthesisView?> GetLastSynthesisAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    Task SaveCompatibilityCriteriaAsync(int candidateProfileId, CandidateCompatibilityCriteriaView criteria, CancellationToken cancellationToken = default);

    /// <summary>Utilisé exclusivement par le Moteur de Compatibilité pour lire les critères déclarés par le candidat.</summary>
    Task<CandidateCompatibilityCriteriaView?> GetCompatibilityCriteriaAsync(int candidateProfileId, CancellationToken cancellationToken = default);
}
