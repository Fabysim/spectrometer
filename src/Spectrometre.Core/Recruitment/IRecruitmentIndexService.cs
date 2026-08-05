namespace Spectrometre.Core.Recruitment;

public sealed record PosteIndexView(int CompanyId, string CompanyName, int PosteId, string Titre, string? Description, string? Departement, string Statut);

public sealed record CandidatureIndexView(int CompanyId, int PosteId, string PosteTitre, int CandidateProfileId, string Statut, int? ScoreCompatibilite, IReadOnlyList<string> TagsCles, DateTimeOffset UpdatedAt);

/// <summary>
/// Point d'entrée public de l'index partagé de recrutement (schéma <c>core</c>). Vit dans le noyau —
/// pas dans un module — car il est écrit par <c>Spectrometre.Modules.PostesRecrutement</c> et lu à la
/// fois par le candidat (<c>/candidat/postes</c>) et par <c>Spectrometre.Modules.Vivier</c> : le noyau
/// est le seul endroit que tous les modules concernés peuvent référencer sans dépendance croisée entre
/// modules (interdite par l'architecture).
/// </summary>
/// <remarks>
/// Stratégie de synchronisation : mise à jour ÉVÉNEMENTIELLE SYNCHRONE — chaque upsert est appelé
/// directement par <c>PosteService</c> dans la foulée de l'écriture qui le déclenche (création/modification
/// de poste, candidature créée, statut changé, score recalculé), dans la continuité de la même requête.
/// Pas de recalcul à la volée à chaque lecture (ce serait revenir à l'itération coûteuse que cet index
/// remplace), pas de file de messages ni de job de réconciliation en arrière-plan (plus sophistiqué que
/// nécessaire pour ce cycle). Limite acceptée : si l'upsert de l'index échoue APRÈS que l'écriture source
/// a réussi, l'index peut devenir temporairement obsolète — un vrai système de production voudrait une
/// transaction distribuée ou un outbox pattern ; hors scope ici (voir le résumé final pour cette décision).
/// </remarks>
public interface IRecruitmentIndexService
{
    Task UpsertPosteAsync(int companyId, string companyName, int posteId, string titre, string? description, string? departement, string statut, CancellationToken cancellationToken = default);

    Task UpsertCandidatureAsync(int companyId, int posteId, string posteTitre, int candidateProfileId, string statut, int? scoreCompatibilite, IReadOnlyList<string> tagsCles, CancellationToken cancellationToken = default);

    /// <summary>Tous les postes au statut "Ouvert", tous tenants confondus — remplace l'itération schéma par schéma.</summary>
    Task<IReadOnlyList<PosteIndexView>> GetPostesOuvertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Postes auxquels ce candidat a déjà postulé, tous tenants confondus — identifiés par la paire
    /// (CompanyId, PosteId), PAS par le seul PosteId : c'est un identifiant auto-incrémenté LOCAL à
    /// chaque schéma tenant (voir <c>Poste.Id</c>), donc deux entreprises différentes peuvent tout à fait
    /// avoir chacune un poste "PosteId=1" — comparer uniquement sur PosteId ferait passer une candidature
    /// chez une entreprise pour une candidature chez une autre.
    /// </summary>
    Task<IReadOnlyList<(int CompanyId, int PosteId)>> GetPostesAvecCandidatureAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Candidatures reçues par une entreprise, toutes confondues (tous postes) — c'est la lecture que fait
    /// le Vivier. Ne retourne QUE des candidats ayant une candidature réelle (voir la contrainte de
    /// confidentialité sur le module Vivier) : jamais un accès plus large au profil candidat.
    /// </summary>
    Task<IReadOnlyList<CandidatureIndexView>> GetCandidaturesPourEntrepriseAsync(int companyId, CancellationToken cancellationToken = default);
}
