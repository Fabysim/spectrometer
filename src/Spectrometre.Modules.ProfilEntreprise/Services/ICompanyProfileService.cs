using Spectrometre.Modules.ProfilEntreprise.Entities;

namespace Spectrometre.Modules.ProfilEntreprise.Services;

public sealed record CompanyQuestionView(int QuestionId, CompanyTheme Theme, int Number, string Text, string? AnswerText, DateTimeOffset? UpdatedAt);

/// <summary>Voir le commentaire équivalent côté Profil Candidat : critères structurés pour le scoring, notes libres hors calcul.</summary>
public sealed record CompanyCompatibilityCriteriaView(
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
