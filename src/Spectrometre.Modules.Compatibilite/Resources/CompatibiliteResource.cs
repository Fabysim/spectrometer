namespace Spectrometre.Modules.Compatibilite.Resources;

/// <summary>
/// Marqueur pour <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> — ressources FR/EN du
/// contenu métier statique de ce module (voir CompatibiliteResource.resx / .en.resx). Vit dans ce module
/// (jamais dans Host ni Core) pour ne créer aucune dépendance Host → module ni inter-module, cohérent avec
/// SharedResource (Host) au cycle de bilinguisme précédent. Pas de ResourcesPath explicite : ce type vit déjà
/// dans le dossier/namespace Resources/, qui correspond directement au nom de ressource intégré par défaut
/// (voir la remarque équivalente dans Program.cs du Host — fixer ResourcesPath="Resources" ici doublerait ce
/// segment et ferait échouer silencieusement toute résolution).
/// </summary>
public sealed class CompatibiliteResource;
