using Spectrometre.Core.Invitations;
using Spectrometre.Modules.ProfilEntreprise.Entities;

namespace Spectrometre.Modules.ProfilEntreprise.Services;

public sealed record PosteView(
    int Id,
    string Titre,
    string? Description,
    string? Departement,
    PosteStatut Statut,
    DateTimeOffset CreatedAt,
    string? TachesDescription = null,
    string? Salaire = null,
    string? Avantages = null,
    DateTimeOffset? DateCloture = null);

/// <summary>Vue d'un poste ouvert pour la recherche côté candidat — regroupe le poste et l'entreprise qui l'a publié, puisque cette vue traverse plusieurs schémas tenant.
/// <see cref="ScoreCompatibilite"/> est renseigné côté page (pas par <c>GetPostesOuvertsAsync</c>) : signal informatif uniquement, jamais un filtre de candidature.</summary>
public sealed record PosteOuvertView(int CompanyId, string CompanyName, int PosteId, string Titre, string? Description, string? Departement, bool DejaPostule, int? ScoreCompatibilite = null);

/// <summary>
/// Détail d'un poste ouvert pour un candidat — <see cref="OffreTexte"/> peut être null si jamais généré.
/// Retourné uniquement si le poste est ouvert ; fermé/inexistant → null uniforme.
/// </summary>
public sealed record PosteDetailCandidatView(
    int CompanyId,
    string CompanyName,
    int PosteId,
    string Titre,
    string? Departement,
    string? OffreTexte,
    DateTimeOffset? OffreGenereeLe,
    bool OffreGenereeParIa,
    bool DejaPostule);

/// <summary>
/// <see cref="ScoreCompatibilite"/> n'est renseigné que si le module Compatibilité est actif pour ce
/// tenant (intégration légère, sans dépendance dure au manifeste — voir <c>ServiceCollectionExtensions</c>).
/// </summary>
public sealed record CandidatureView(int Id, int PosteId, int CandidateProfileId, CandidatureStatut Statut, DateTimeOffset CreatedAt, int? ScoreCompatibilite, bool EstPreselectionne);

/// <summary>Critère de compétence d'un poste (profil exigé), tenant ambiant.</summary>
public sealed record CritereEvaluationView(
    int Id,
    int PosteId,
    string Categorie,
    string Libelle,
    NiveauEvaluation NiveauRequis,
    int OrdreAffichage);

/// <summary>
/// Ligne d'évaluation d'un critère pour une candidature.
/// <see cref="NiveauDeclare"/> vient de l'auto-évaluation candidat à la postulation ;
/// <see cref="NiveauFinal"/> est null tant que l'entreprise n'a pas ajusté le niveau.
/// </summary>
public sealed record EvaluationCritereView(
    int CritereId,
    string Categorie,
    string Libelle,
    NiveauEvaluation NiveauRequis,
    NiveauEvaluation? NiveauDeclare,
    NiveauEvaluation? NiveauFinal,
    int OrdreAffichage);

/// <summary>
/// Point d'entrée public du module Postes & Recrutement. Deux familles de méthodes : côté entreprise
/// (tenant ambiant, comme Profil Entreprise) et côté candidat (traversée explicite de plusieurs
/// schémas tenant, puisque parcourir les postes ouverts n'est pas une opération mono-tenant).
/// </summary>
public interface IPosteService
{
    // --- Côté entreprise (tenant actif via ITenantContext) ---
    Task<int> CreatePosteAsync(
        string titre,
        string? description,
        string? departement,
        string? tachesDescription = null,
        string? salaire = null,
        string? avantages = null,
        DateTimeOffset? dateCloture = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosteView>> GetPostesAsync(CancellationToken cancellationToken = default);
    Task UpdatePosteAsync(
        int posteId,
        string titre,
        string? description,
        string? departement,
        string? tachesDescription = null,
        string? salaire = null,
        string? avantages = null,
        DateTimeOffset? dateCloture = null,
        CancellationToken cancellationToken = default);
    Task SetPosteStatutAsync(int posteId, PosteStatut statut, CancellationToken cancellationToken = default);

    /// <summary>
    /// Supprime le poste du tenant actif (candidatures, critères, guides, index recrutement, invitations
    /// candidat en attente). No-op s'il est introuvable dans ce schéma.
    /// </summary>
    Task DeletePosteAsync(int posteId, CancellationToken cancellationToken = default);

    /// <summary>Inclut le score de compatibilité par candidature si le module Compatibilité est actif pour ce tenant.</summary>
    Task<IReadOnlyList<CandidatureView>> GetCandidaturesAsync(int posteId, CancellationToken cancellationToken = default);

    /// <summary>Une candidature du tenant actif, ou null si introuvable.</summary>
    Task<CandidatureView?> GetCandidatureAsync(int candidatureId, CancellationToken cancellationToken = default);

    Task SetCandidatureStatutAsync(int candidatureId, CandidatureStatut statut, CancellationToken cancellationToken = default);

    /// <summary>Marque ou démarque une candidature comme présélectionnée (shortlist). No-op si introuvable dans le tenant actif.</summary>
    Task SetPreselectionAsync(int candidatureId, bool preselectionne, CancellationToken cancellationToken = default);

    /// <summary>Critères d'évaluation du poste dans le tenant actif (vide si le poste n'existe pas dans ce schéma).</summary>
    Task<IReadOnlyList<CritereEvaluationView>> GetCriteresAsync(int posteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Critères d'un poste ouvert pour un candidat (schéma de l'entreprise <paramref name="companyId"/>).
    /// Vide si le poste est introuvable ou fermé — même politique d'uniformité que
    /// <see cref="GetPosteOuvertDetailAsync"/>.
    /// </summary>
    Task<IReadOnlyList<CritereEvaluationView>> GetCriteresPosteOuvertAsync(
        int companyId,
        int posteId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crée ou met à jour un critère du poste. <paramref name="niveauRequis"/> est clampé sur 0–4
    /// (échelle MVP). No-op si le poste n'existe pas dans le tenant actif, ou si <paramref name="id"/>
    /// pointe un critère d'un autre poste.
    /// </summary>
    Task UpsertCritereAsync(int posteId, int? id, string categorie, string libelle, int niveauRequis, int ordreAffichage, CancellationToken cancellationToken = default);

    /// <summary>Supprime un critère du tenant actif — no-op s'il est introuvable dans ce schéma.</summary>
    Task DeleteCritereAsync(int critereId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grille d'évaluation d'une candidature : un élément par critère du poste, avec
    /// <see cref="EvaluationCritereView.NiveauFinal"/> null si jamais évalué. Vide si la candidature
    /// est introuvable dans le tenant actif.
    /// </summary>
    Task<IReadOnlyList<EvaluationCritereView>> GetEvaluationCriteresAsync(int candidatureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enregistre (upsert) le niveau final d'un critère pour une candidature. <paramref name="niveauFinal"/>
    /// est clampé sur 0–4. No-op si la candidature ou le critère (du même poste) est introuvable.
    /// </summary>
    Task SetNiveauFinalAsync(int candidatureId, int critereId, int niveauFinal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Génère des critères via IA et les agrège aux critères existants (jamais d'écrasement).
    /// Idempotent via hash du contexte poste (titre/description/tâches/compétences) sauf
    /// <paramref name="forcerRegeneration"/>. Jamais d'exception remontée.
    /// </summary>
    /// <returns>
    /// Nombre de critères ajoutés ; <c>0</c> si le contexte est inchangé ;
    /// <c>-1</c> si l'IA a échoué (hash non mis à jour, critères existants intacts).
    /// </returns>
    Task<int> GenererCriteresIaAsync(int posteId, bool forcerRegeneration = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invite un candidat par email à postuler sur le poste (tenant actif).
    /// <see cref="Invitation.ContextId"/> = <paramref name="posteId"/>. L'émetteur doit être rattaché à l'entreprise active.
    /// </summary>
    Task<Invitation> InviterCandidatAsync(int posteId, string email, string emetteurUserId, CancellationToken cancellationToken = default);

    /// <summary>Invitations en attente pour ce poste (email, date, lien relatif d'acceptation).</summary>
    Task<IReadOnlyList<InvitationView>> GetInvitationsCandidatEnCoursAsync(int posteId, CancellationToken cancellationToken = default);

    /// <summary>Révoque une invitation candidat en attente — seul l'émetteur peut révoquer.</summary>
    Task RevokerInvitationCandidatAsync(int invitationId, string requestingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalise une invitation <see cref="InvitationType.CandidaturePoste"/> : résout le profil candidat
    /// de l'accepteur et crée la candidature sur le poste (idempotent si déjà postulé).
    /// </summary>
    Task FinaliserCandidatureDepuisInvitationAsync(Invitation invitation, string accepteurUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rattache au poste un candidat DÉJÀ présent dans le vivier de l'entreprise active (au moins une
    /// candidature indexée ailleurs dans cette entreprise via <c>IRecruitmentIndexService</c>).
    /// Idempotent si la candidature (poste, candidat) existe déjà. Lève
    /// <see cref="InvalidOperationException"/> si le poste est introuvable dans le tenant actif, ou si
    /// le candidat n'a aucune candidature connue pour cette entreprise.
    /// </summary>
    Task<int> RattacherCandidatDepuisVivierAsync(int posteId, int candidateProfileId, CancellationToken cancellationToken = default);

    // --- Côté candidat (traverse tous les tenants) ---
    Task<IReadOnlyList<PosteOuvertView>> GetPostesOuvertsAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Détail d'un poste ouvert pour un candidat (paire CompanyId+PosteId). Retourne <c>null</c> de façon
    /// uniforme si le poste n'existe pas OU s'il est fermé — jamais de distinction qui confirmerait
    /// l'existence d'un poste fermé.
    /// </summary>
    Task<PosteDetailCandidatView?> GetPosteOuvertDetailAsync(
        int companyId,
        int posteId,
        int candidateProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crée une candidature sans grille déclarée (invitation acceptée, rattachement vivier, tests).
    /// Le parcours UI « Postuler » doit utiliser <see cref="PostulerAvecGrilleAsync"/>.
    /// </summary>
    Task PostulerAsync(int companyId, int posteId, int candidateProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Postulation atomique avec grille complète : refuse si un critère du poste manque dans
    /// <paramref name="niveauxDeclares"/> ; sinon crée la candidature et une ligne
    /// <c>EvaluationCritereCandidature</c> par critère (<see cref="EvaluationCritereCandidature.NiveauDeclare"/>
    /// renseigné, <see cref="EvaluationCritereCandidature.NiveauFinal"/> null).
    /// Idempotent si déjà postulé (succès sans erreur).
    /// </summary>
    Task<(bool Succes, string? Erreur)> PostulerAvecGrilleAsync(
        int companyId,
        int posteId,
        int candidateProfileId,
        IReadOnlyDictionary<int, NiveauEvaluation> niveauxDeclares,
        CancellationToken cancellationToken = default);
}

/// <summary>Invitation candidat en cours (affichage côté entreprise).</summary>
public sealed record InvitationView(
    int Id,
    string EmailInvite,
    DateTimeOffset CreatedAt,
    string LienRelatif);
