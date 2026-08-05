using Spectrometre.Modules.ProfilEntreprise.Entities;

namespace Spectrometre.Modules.ProfilEntreprise.Services;

public sealed record CompanyQuestionView(int QuestionId, CompanyTheme Theme, int Number, string Text, string? AnswerText, DateTimeOffset? UpdatedAt);

public sealed record CompanyCompatibilityCriteriaView(
    string? TechniqueText,
    string? ComportementaleText,
    string? CulturelleText,
    string? OrganisationnelleText,
    string? MotivationnelleText,
    string? PointsVigilanceText);

/// <summary>
/// Point d'entrée public du module Profil Entreprise, pour le schéma de l'entreprise active
/// (<see cref="Spectrometre.Core.Tenancy.ITenantContext"/>). Le module Compatibilité passe
/// exclusivement par cette interface.
/// </summary>
public interface ICompanyProfileService
{
    /// <summary>Une seule ligne par schéma : le profil de l'entreprise active. Créée à la première utilisation.</summary>
    Task<int> GetOrCreateProfileIdAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyQuestionView>> GetQuestionnaireAsync(int companyProfileId, CancellationToken cancellationToken = default);

    Task SaveAnswerAsync(int companyProfileId, int questionId, string? answerText, CancellationToken cancellationToken = default);

    Task SaveCompatibilityCriteriaAsync(int companyProfileId, CompanyCompatibilityCriteriaView criteria, CancellationToken cancellationToken = default);

    /// <summary>Utilisé exclusivement par le Moteur de Compatibilité pour lire les critères déclarés par l'entreprise.</summary>
    Task<CompanyCompatibilityCriteriaView?> GetCompatibilityCriteriaAsync(int companyProfileId, CancellationToken cancellationToken = default);
}
