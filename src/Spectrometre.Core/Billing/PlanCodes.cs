namespace Spectrometre.Core.Billing;

/// <summary>
/// Libellés de bundle connus pour les seeds et les tests — <see cref="TenantSubscription.PlanCode"/>/
/// <see cref="CandidateSubscription.PlanCode"/>/<see cref="CoachSubscription.PlanCode"/> restent des
/// chaînes libres (étiquette informative du bundle de départ, pas de FK ni de gating). Ces constantes
/// évitent seulement de disperser les mêmes littéraux. La facturation à la carte vit dans
/// <see cref="ModulePrix"/> ; l'accès effectif aux modules dépend de l'activation + statut d'abonnement
/// (Essai/Active), pas de ces codes.
/// </summary>
public static class PlanCodes
{
    /// <summary>Étiquette historique Matching Emploi (sans Gestion du temps).</summary>
    public const string Standard = "Standard";

    /// <summary>Étiquette historique Standard + Gestion du temps.</summary>
    public const string StandardPlusTemps = "StandardPlusTemps";

    /// <summary>Étiquette historique du profil Coach (socle ProfilCoach).</summary>
    public const string Coach = "Coach";

    /// <summary>Étiquette historique Coach + Gestion du temps.</summary>
    public const string CoachPlusTemps = "CoachPlusTemps";

    /// <summary>Étiquette du profil Particulier (publication de missions).</summary>
    public const string Particulier = "Particulier";
}
