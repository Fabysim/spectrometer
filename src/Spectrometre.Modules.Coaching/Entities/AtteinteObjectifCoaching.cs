namespace Spectrometre.Modules.Coaching.Entities;

/// <summary>
/// Atteinte d'un objectif de coaching. Équivalent sémantique de <c>AtteinteObjectif</c> (SuiviEmployes) —
/// dupliqué ici volontairement : aucun enum partageable n'existe dans Core, et Coaching ne doit pas
/// référencer les entités SuiviEmployes.
/// </summary>
public enum AtteinteObjectifCoaching
{
    NonDefini = 0,
    Oui = 1,
    Non = 2,
    NonImputable = 3,
}
