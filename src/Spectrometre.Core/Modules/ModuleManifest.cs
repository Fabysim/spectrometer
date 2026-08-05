namespace Spectrometre.Core.Modules;

/// <summary>
/// Manifeste minimal d'un module (nom, version, dépendances), sur le modèle des « Apps » Odoo.
/// Chaque module l'enregistre auprès d'<see cref="IModuleRegistry"/> depuis sa méthode d'extension DI
/// (ex. <c>AddProfilCandidatModule</c>).
/// </summary>
/// <param name="Code">Identifiant technique stable du module (ex. <c>"ProfilCandidat"</c>).</param>
/// <param name="DisplayName">Nom affiché à l'utilisateur.</param>
/// <param name="Version">Version du module, indépendante des autres modules et du noyau.</param>
/// <param name="RequiredModuleCodes">Codes des modules qui doivent être actifs pour qu'un tenant puisse activer celui-ci.</param>
public sealed record ModuleManifest(
    string Code,
    string DisplayName,
    string Version,
    IReadOnlyList<string> RequiredModuleCodes);
