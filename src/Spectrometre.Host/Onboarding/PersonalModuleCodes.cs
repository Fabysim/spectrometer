namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Modules à schéma global scopé par <c>UserId</c> (Profil Candidat aujourd'hui) — jamais pertinents pour
/// une ENTREPRISE, même si une dépendance de manifeste passée (voir le commentaire sur
/// <c>Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest</c>, désormais corrigée) ou
/// une donnée déjà en base a pu les marquer actifs pour ce sujet. Filtre défensif utilisé UNIQUEMENT côté
/// affichage (menu, tableau de bord, écran Ajouter un module) pour le sujet Company — protège aussi les
/// entreprises créées avant ce correctif, sans toucher à la mécanique d'activation elle-même (une ligne
/// <c>ModuleActivation</c> existante n'est jamais supprimée par ce filtre, seulement ignorée à l'affichage).
/// </summary>
/// <remarks>
/// Gestion du temps n'y figure PAS : contrairement à Profil Candidat, c'est un module réellement
/// activable pour une entreprise (voir <see cref="CompanyOnboardingService.ActivateGestionDuTempsAsync"/>)
/// — seule son activation côté CANDIDAT est personnelle, et elle est déjà scopée séparément
/// (<c>IsActiveForCandidateAsync</c> avec un <c>candidateProfileId</c>, jamais un <c>companyId</c>).
/// </remarks>
public static class PersonalModuleCodes
{
    public static readonly IReadOnlySet<string> Codes = new HashSet<string> { "ProfilCandidat", "ProfilParticulier" };
}
