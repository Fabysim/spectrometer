using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>Vue exposée publiquement d'une question avec la réponse courante du candidat, si elle existe.</summary>
public sealed record CandidateQuestionView(int QuestionId, CandidateTheme Theme, int Number, string Text, IReadOnlyList<string> Examples, string? AnswerText, DateTimeOffset? UpdatedAt);

public sealed record CandidateSynthesisView(IReadOnlyDictionary<SynthesisCategory, IReadOnlyList<string>> TagsByCategory, DateTimeOffset GeneratedAt);

public sealed record CandidateCompatibilityCriteriaView(
    string? TechniqueText,
    string? ComportementaleText,
    string? CulturelleText,
    string? OrganisationnelleText,
    string? MotivationnelleText,
    string? PointsVigilanceText);

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
