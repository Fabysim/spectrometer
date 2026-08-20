namespace Spectrometre.Core.Modules;

/// <summary>
/// « Sujet » auquel un module peut être activé — généralisation introduite pour que l'activation de
/// modules ne soit plus couplée à la seule notion d'entreprise (voir <see cref="ModuleActivation"/>).
/// Stocké comme entier (conversion par défaut d'EF Core) : ajouter <c>Independant</c> plus tard (futur
/// profil « indépendant », pas construit dans ce cycle) ne demandera qu'une nouvelle valeur d'énumération
/// C#, aucune migration structurelle — la colonne existe déjà et n'importe quelle valeur entière y tient.
/// <see cref="Coach"/> ajouté au cycle Coaching, même principe.
/// <see cref="Particulier"/> ajouté au cycle Missions — acteur de plein droit (inscription libre-service).
/// </summary>
public enum ModuleActivationSubjectType
{
    Company = 0,
    Candidate = 1,
    Coach = 2,
    Particulier = 3,
}
