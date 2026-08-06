namespace Spectrometre.Host.Resources;

/// <summary>
/// Marqueur pour <c>IStringLocalizer&lt;SharedResource&gt;</c> — un seul jeu de ressources partagé pour tout
/// le « chrome » de l'application (connexion, inscription, menu, tableau de bord, écrans Ajouter un module),
/// plutôt qu'un fichier par composant. Vit dans Host (jamais dans un module) : aucun module ne référence ce
/// type, donc aucune dépendance inter-module créée par la localisation.
/// </summary>
/// <remarks>
/// Ne couvre PAS le contenu métier des modules (questionnaire candidat, grilles H/K, formulaire de CV,
/// Gestion du temps, etc.) — volontairement laissé en français pour ce cycle, voir <c>SharedResource.resx</c>.
/// </remarks>
public sealed class SharedResource;
