namespace Spectrometre.Core.Billing;

/// <summary>
/// Codes de plan connus, pour les seeds et les tests — <see cref="TenantSubscription.PlanCode"/>/
/// <see cref="CandidateSubscription.PlanCode"/>/<see cref="CoachSubscription.PlanCode"/> restent des
/// chaînes libres (référence molle vers <see cref="Plan.Code"/>, pas de FK) ; ces constantes évitent
/// seulement de disperser les mêmes littéraux entre le seed et les tests. Les prix vivent dans
/// <see cref="Plan"/> (éditables via <c>/admin/plans</c>).
/// </summary>
public static class PlanCodes
{
    /// <summary>Tous les modules du domaine Matching Emploi — sans Gestion du temps, vendu séparément.</summary>
    public const string Standard = "Standard";

    /// <summary>Standard + Gestion du temps.</summary>
    public const string StandardPlusTemps = "StandardPlusTemps";

    /// <summary>Plan unique, gratuit, du profil Coach — inclut uniquement ProfilCoach.</summary>
    public const string Coach = "Coach";

    /// <summary>Coach + Gestion du temps (usage personnel du coach) — voir <c>CoachOnboardingService.ActivateGestionDuTempsAsync</c>.</summary>
    public const string CoachPlusTemps = "CoachPlusTemps";
}
