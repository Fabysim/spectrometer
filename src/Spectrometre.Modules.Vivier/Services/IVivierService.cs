using Spectrometre.Modules.ProfilCandidat.Services;

namespace Spectrometre.Modules.Vivier.Services;

public sealed record CandidatureVivierView(int PosteId, string PosteTitre, int CandidateProfileId, string Statut, int? ScoreCompatibilite, IReadOnlyList<string> TagsCles, DateTimeOffset UpdatedAt);

/// <summary>
/// Détail complet exposé à une entreprise pour UN candidat ayant réellement postulé — critères de
/// compatibilité (déjà exposés) et CV (ajouté pour l'affichage côté entreprise, voir la demande d'origine :
/// jamais un nouveau chemin d'accès parallèle, seulement une extension de CET accesseur déjà sécurisé).
/// Le CV reste un contenu informatif ici, jamais un facteur de scoring — aucun impact sur le Moteur de
/// Compatibilité, qui continue de ne lire que <see cref="Criteres"/> via sa propre voie existante.
/// </summary>
public sealed record VivierCandidateDetailView(CandidateCompatibilityCriteriaView Criteres, CvView Cv);

/// <summary>
/// Point d'entrée public du module Vivier — un pur filtre de lecture sur l'index partagé de recrutement,
/// jamais un accès plus large au vivier de profils candidats.
/// </summary>
/// <remarks>
/// Contrainte de confidentialité (rappel explicite du cycle) : le Vivier ne doit donner accès qu'aux
/// candidats ayant déjà postulé à un poste de l'entreprise active, jamais à l'ensemble des profils
/// candidats de la base. C'est un garde-fou structurel ici, pas une simple option d'affichage : les deux
/// méthodes de ce service passent EXCLUSIVEMENT par
/// <c>Spectrometre.Core.Recruitment.IRecruitmentIndexService.GetCandidaturesPourEntrepriseAsync</c>, qui ne
/// contient par construction que des candidats ayant une candidature réelle (voir le commentaire sur
/// <c>CandidatureIndexEntry</c>) — ce service n'interroge JAMAIS <c>ICandidateProfileService</c> pour lister
/// des candidats, seulement pour lire le détail d'UN candidat déjà confirmé comme ayant postulé
/// (<see cref="GetCandidateDetailAsync"/> vérifie cette condition avant tout accès, y compris contre un
/// accès direct par URL).
/// </remarks>
public interface IVivierService
{
    /// <summary>Toutes les candidatures reçues par l'entreprise active, tous postes confondus — le filtrage (score, tags) est fait côté page.</summary>
    Task<IReadOnlyList<CandidatureVivierView>> GetCandidaturesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Détail des critères déclarés par un candidat (grille H), UNIQUEMENT s'il a une candidature vers
    /// l'entreprise active — <c>null</c> sinon (candidat inexistant, jamais postulé ici, ou entreprise
    /// différente), y compris en cas d'accès direct par URL avec un identifiant arbitraire.
    /// </summary>
    Task<VivierCandidateDetailView?> GetCandidateDetailAsync(int candidateProfileId, CancellationToken cancellationToken = default);
}
