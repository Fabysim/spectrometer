namespace Spectrometre.Modules.SuiviEvolutif.Services;

/// <summary><see cref="Description"/> est déjà en langage courant (voir l'implémentation) — jamais un nom de champ technique brut.</summary>
public sealed record HistoriqueEntreeView(string Description, string? AncienneValeur, string? NouvelleValeur, DateTimeOffset Horodatage);

/// <summary>
/// Point d'entrée public du module Suivi Évolutif, en LECTURE seule (l'écriture passe par
/// <c>IProfileChangeRecorder</c>, appelé depuis ProfilCandidat/ProfilEntreprise).
/// </summary>
/// <remarks>
/// Pas de garde d'autorisation interne à ce service — même principe que <c>ICandidateProfileService.GetQuestionnaireAsync</c>
/// ou <c>ICompanyProfileService.GetQuestionnaireAsync</c> : l'appelant (page Razor) ne résout et ne passe
/// jamais qu'un identifiant qui lui appartient déjà légitimement (le candidat consulte SON PROPRE profil
/// résolu depuis ses claims, le manager consulte l'entreprise déjà sélectionnée parmi celles qu'il gère) —
/// exactement le pattern déjà en place pour ces pages, pas un nouveau mécanisme d'accès par identifiant
/// arbitraire comme /compatibilite/resultat/{id} (qui, lui, a besoin d'un accesseur sécurisé dédié).
/// </remarks>
public interface ISuiviEvolutifService
{
    Task<IReadOnlyList<HistoriqueEntreeView>> GetHistoriqueCandidatAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoriqueEntreeView>> GetHistoriqueEntrepriseAsync(int companyProfileId, CancellationToken cancellationToken = default);
}
