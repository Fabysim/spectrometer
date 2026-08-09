namespace Spectrometre.Core.Billing;

/// <summary>
/// Codes de plan connus, pour les seeds et les tests — <see cref="TenantSubscription.PlanCode"/>/
/// <see cref="CandidateSubscription.PlanCode"/> restent des chaînes libres (même simplicité que
/// <c>ModuleCode</c> ailleurs, pas de table <c>Plan</c> séparée pour ce cycle) ; ces constantes évitent
/// seulement de disperser les mêmes littéraux entre le seed et les tests.
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
