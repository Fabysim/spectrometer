namespace Spectrometre.Core.Identity;

/// <summary>
/// Rôles ASP.NET Core Identity de la plateforme — jusqu'ici <c>AddRoles&lt;IdentityRole&gt;()</c> était
/// enregistré (voir <c>ServiceCollectionExtensions.AddSpectrometreCore</c>) mais jamais réellement utilisé :
/// toute l'autorisation existante passe par des tables métier dédiées (<c>UserCompanyLink</c>,
/// <c>LienCoaching</c>...), jamais par les rôles Identity. <see cref="Admin"/> est le premier usage réel.
/// </summary>
public static class PlatformRoles
{
    /// <summary>Accès à la zone transverse <c>/admin</c> (voir <c>Spectrometre.Modules.Admin</c>) — jamais un sujet du registre d'activation généralisé, voir <see cref="Modules.ModuleActivationSubjectType"/>.</summary>
    public const string Admin = "PlatformAdmin";
}
