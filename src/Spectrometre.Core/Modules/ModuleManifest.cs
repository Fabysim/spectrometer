namespace Spectrometre.Core.Modules;

/// <summary>
/// Manifeste minimal d'un module (nom, version, dépendances), sur le modèle des « Apps » Odoo.
/// Chaque module l'enregistre auprès d'<see cref="IModuleRegistry"/> depuis sa méthode d'extension DI
/// (ex. <c>AddProfilCandidatModule</c>).
/// </summary>
/// <param name="Code">Identifiant technique stable du module (ex. <c>"ProfilCandidat"</c>).</param>
/// <param name="DisplayName">Nom affiché à l'utilisateur (français, culture par défaut).</param>
/// <param name="DisplayNameEn">
/// Nom affiché en anglais (menu/tableau de bord uniquement — voir le cycle de bilinguisme). Traduction
/// automatique pour l'instant, à affiner par une relecture humaine dans un cycle ultérieur, au même titre
/// que <c>SharedResource.en.resx</c>. Ne couvre pas le contenu métier interne du module.
/// </param>
/// <param name="Version">Version du module, indépendante des autres modules et du noyau.</param>
/// <param name="RequiredModuleCodes">Codes des modules qui doivent être actifs pour qu'un tenant puisse activer celui-ci.</param>
public sealed record ModuleManifest(
    string Code,
    string DisplayName,
    string DisplayNameEn,
    string Version,
    IReadOnlyList<string> RequiredModuleCodes);
