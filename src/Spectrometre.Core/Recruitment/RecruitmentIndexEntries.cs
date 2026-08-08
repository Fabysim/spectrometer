namespace Spectrometre.Core.Recruitment;

/// <summary>
/// Index de lecture rapide, dénormalisé, dans le schéma partagé <c>core</c> : un poste ouvert par une
/// entreprise, consultable sans traverser les schémas tenant un par un. Alimenté par
/// <c>Spectrometre.Modules.Recrutement.Services.PosteService</c> à chaque création/modification/
/// changement de statut d'un poste (voir <see cref="IRecruitmentIndexService"/>).
/// </summary>
/// <remarks>
/// Ne remplace PAS la table <c>Postes</c> du schéma tenant (qui reste la source de vérité) — c'est une
/// copie de lecture, volontairement minimale (juste ce qu'il faut pour lister/filtrer côté candidat).
/// <see cref="Statut"/> est une chaîne (pas l'enum <c>PosteStatut</c> du module PostesRecrutement) : le
/// noyau ne doit dépendre d'AUCUN module (voir l'architecture), donc pas de référence à un type qui y vit.
/// </remarks>
public sealed class PosteIndexEntry
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public required string CompanyName { get; set; }
    public int PosteId { get; set; }
    public required string Titre { get; set; }
    public string? Description { get; set; }
    public string? Departement { get; set; }
    public required string Statut { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Index de lecture rapide d'une candidature : poste + candidat + score de compatibilité + tags clés +
/// statut. C'est LA table que consulte le module Vivier — jamais directement les schémas tenant ni le
/// profil candidat global (voir la contrainte de confidentialité sur <c>Spectrometre.Modules.Vivier</c> :
/// un candidat n'apparaît ici QUE s'il a une candidature réelle, cette table ne référence jamais des
/// candidats qui n'ont postulé nulle part). Alimentée à chaque candidature créée, changement de statut,
/// ou recalcul de score de compatibilité (voir <c>PosteService</c>).
/// </summary>
/// <remarks>
/// L'unicité logique d'une ligne est la paire (<see cref="CompanyId"/>, <see cref="PosteId"/>,
/// <see cref="CandidateProfileId"/>) — PAS (PosteId, CandidateProfileId) seul : <see cref="PosteId"/> est un
/// identifiant auto-incrémenté LOCAL à chaque schéma tenant (voir <c>Poste.Id</c>), donc deux entreprises
/// différentes ont chacune leur propre "PosteId=1". Sans <see cref="CompanyId"/> dans la clé, la candidature
/// d'un même candidat chez deux entreprises différentes ayant par coïncidence le même PosteId local
/// s'écraserait mutuellement dans cet index.
/// </remarks>
public sealed class CandidatureIndexEntry
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int PosteId { get; set; }
    public required string PosteTitre { get; set; }
    public int CandidateProfileId { get; set; }
    public required string Statut { get; set; }
    public int? ScoreCompatibilite { get; set; }
    public List<string> TagsCles { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Champs ajoutés pour le tableau de bord Analytics (module Analytics/Décideurs) : mêmes 5 axes que
    // CompatibilityResult (Compatibilite), en propriétés à plat plutôt qu'un dictionnaire par CompatibilityAxis —
    // le noyau ne référence aucun module, donc pas de type d'axe venant de Compatibilite ici (voir le
    // commentaire sur PosteIndexEntry.Statut pour la même contrainte appliquée au statut). Nuls tant que
    // Compatibilite n'est pas actif pour ce tenant ou que le score n'a pas encore été calculé pour cette
    // candidature — même sémantique que ScoreCompatibilite.
    public int? ScoreTechnique { get; set; }
    public int? ScoreComportementale { get; set; }
    public int? ScoreCulturelle { get; set; }
    public int? ScoreOrganisationnelle { get; set; }
    public int? ScoreMotivationnelle { get; set; }

    /// <summary>
    /// Tags de vigilance PARTAGÉS (signalés à la fois par le candidat et l'entreprise, voir
    /// <c>StructuredCriteriaScorer.SharedVigilanceTags</c>) — distinct de <see cref="TagsCles"/> qui contient
    /// les compétences techniques du candidat. Alimente le "top des points de vigilance" du tableau de bord
    /// Analytics sans reparser les phrases déjà formatées de <c>CompatibiliteResultView.PointsDeVigilance</c>.
    /// </summary>
    public List<string> PointsVigilanceTags { get; set; } = [];

    /// <summary>
    /// Grille H (critères structurés du candidat) entièrement renseignée — même définition que
    /// <c>QuestionnaireCandidat.razor</c>'s <c>GrilleComplete</c>. Indépendant de l'activation de
    /// Compatibilite pour ce tenant : c'est une donnée du candidat, pas un résultat de calcul.
    /// </summary>
    public bool GrilleCandidatComplete { get; set; }
}
