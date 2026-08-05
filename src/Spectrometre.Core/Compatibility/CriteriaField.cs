namespace Spectrometre.Core.Compatibility;

/// <summary>
/// Les 6 axes des grilles de compatibilité (H côté candidat, K côté entreprise). Partagé entre
/// <c>Spectrometre.Modules.ProfilCandidat</c> et <c>Spectrometre.Modules.ProfilEntreprise</c> pour que
/// leurs services de sauvegarde exposent des méthodes de mutation ciblées par axe (une checkbox cochée =
/// une mutation sur UN axe, pas une réécriture de toute la grille) — voir le correctif de concurrence sur
/// <c>CandidateProfileService</c>/<c>CompanyProfileService</c> : c'est cette granularité qui permet de
/// relire puis réappliquer précisément le changement en cause en cas de conflit d'écriture concurrente.
/// </summary>
public enum CriteriaField
{
    Technique,
    Comportementale,
    Culturelle,

    /// <summary>Pas de tags pour cet axe (uniquement <see cref="CompatibilityVocabulary.RythmeTravailLabels"/> + notes) — voir <c>ToggleTagAsync</c> qui rejette cette valeur.</summary>
    Organisationnelle,
    Motivationnelle,
    PointsVigilance,
}
