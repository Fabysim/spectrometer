namespace Spectrometre.Core.Suivi;

/// <summary>Qui possède le champ modifié — un candidat (schéma fixe) ou une entreprise (schéma tenant).</summary>
public enum ProfileOwnerType
{
    Candidat,
    Entreprise,
}

/// <summary>
/// Abstraction d'inversion de dépendance : <c>ProfilCandidat</c> et <c>ProfilEntreprise</c> doivent tracer
/// les modifications de leurs critères de compatibilité (grilles H/K) pour le module Suivi Évolutif, mais
/// ne doivent JAMAIS référencer <c>Spectrometre.Modules.SuiviEvolutif</c> : le manifeste de ce dernier
/// déclare la dépendance dans l'autre sens (SuiviEvolutif dépend de ProfilCandidat + ProfilEntreprise), et
/// un module ne dépend jamais de ce qui dépend de lui — même recette que
/// <c>Spectrometre.Core.Recruitment.ICandidatureExistenceChecker</c> pour Compatibilite/PostesRecrutement.
/// Cette interface vit dans le noyau, consommée via injection de dépendance ; l'implémentation réelle est
/// fournie par SuiviEvolutif et câblée depuis <c>Spectrometre.Host</c>.
/// </summary>
public interface IProfileChangeRecorder
{
    /// <summary>
    /// Enregistre un changement de valeur sur un champ de profil, si <paramref name="ancienneValeur"/> et
    /// <paramref name="nouvelleValeur"/> diffèrent réellement (l'implémentation par défaut ignore un appel
    /// sans changement effectif). <paramref name="ownerId"/> est l'identifiant LOCAL du profil concerné
    /// (CandidateProfileId ou CompanyProfileId, jamais un identifiant global d'entreprise).
    /// </summary>
    Task RecordChangeAsync(
        int ownerId,
        ProfileOwnerType ownerType,
        string champ,
        string? ancienneValeur,
        string? nouvelleValeur,
        DateTimeOffset horodatage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implémentation par défaut, enregistrée par <c>AddSpectrometreCore</c> — un filet de sécurité si
/// <c>Spectrometre.Host</c> ne branche pas l'implémentation réelle de SuiviEvolutif (ou si ce module n'a
/// pas de sens pour un déploiement donné) : ProfilCandidat/ProfilEntreprise continuent de fonctionner
/// normalement, simplement sans historique tracé. Voir <c>Spectrometre.Modules.SuiviEvolutif.Services.ProfileChangeRecorder</c>
/// pour l'implémentation réelle et sa propre logique de no-op PAR TENANT (module non activé pour
/// l'entreprise active), câblée par-dessus celle-ci depuis <c>Program.cs</c>.
/// </summary>
public sealed class NoOpProfileChangeRecorder : IProfileChangeRecorder
{
    public Task RecordChangeAsync(int ownerId, ProfileOwnerType ownerType, string champ, string? ancienneValeur, string? nouvelleValeur, DateTimeOffset horodatage, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
