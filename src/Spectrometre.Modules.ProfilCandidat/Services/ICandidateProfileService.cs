using Spectrometre.Core.Compatibility;
using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Vue agrégée du CV structuré (sections 1 à 8 du document source). Les entités des sections sont exposées
/// directement plutôt que mirées dans des DTOs séparés — exception délibérée à la convention habituelle
/// (voir <c>Spectrometre.Modules.GestionDuTemps.IGestionDuTempsService</c> pour le même choix et sa
/// justification) : ce sont de purs sacs de champs sans relation à cacher, et dupliquer une quarantaine de
/// champs au total dans des records séparés n'aurait ajouté aucune sécurité, seulement un risque de
/// désynchronisation. L'implémentation ignore toujours l'Id/CandidateProfileId fournis par l'appelant dans
/// les méthodes de sauvegarde et les résout elle-même côté serveur.
/// </summary>
public sealed record CvView(
    CvCoordonnees? Coordonnees,
    IReadOnlyList<CvFormation> Formations,
    CvCompetencesEtudes? CompetencesEtudes,
    IReadOnlyList<CvExperience> Experiences,
    CvCaracteristiquesPersonnelles? CaracteristiquesPersonnelles,
    CvLoisirs? Loisirs,
    IReadOnlyList<CvReference> References,
    CvDeclaration? Declaration);

/// <summary>Vue exposée publiquement d'une question avec la réponse courante du candidat, si elle existe.</summary>
public sealed record CandidateQuestionView(int QuestionId, CandidateTheme Theme, int Number, string Text, IReadOnlyList<string> Examples, string? AnswerText, DateTimeOffset? UpdatedAt);

public sealed record CandidateSynthesisView(IReadOnlyDictionary<SynthesisCategory, IReadOnlyList<string>> TagsByCategory, DateTimeOffset GeneratedAt);

/// <summary>
/// Critères structurés (tags du vocabulaire partagé + échelle) utilisés pour le scoring, accompagnés de
/// notes libres optionnelles pour la nuance humaine — les <c>*Notes</c> ne sont jamais utilisées dans
/// le calcul de compatibilité, uniquement affichées en contexte.
/// </summary>
public sealed record CandidateCompatibilityCriteriaView(
    IReadOnlyList<string> TechniqueTags,
    IReadOnlyList<string> ComportementaleTags,
    IReadOnlyList<string> CulturelleTags,
    int? RythmeTravail,
    IReadOnlyList<string> MotivationnelleTags,
    IReadOnlyList<string> PointsVigilanceTags,
    string? TechniqueNotes,
    string? ComportementaleNotes,
    string? CulturelleNotes,
    string? OrganisationnelleNotes,
    string? MotivationnelleNotes,
    string? PointsVigilanceNotes)
{
    /// <summary>
    /// Grille absente / jamais remplie — tags vides, rythme et notes null.
    /// Utile côté Vivier quand le candidat a postulé mais n'a pas encore déclaré ses critères.
    /// </summary>
    public static CandidateCompatibilityCriteriaView Empty { get; } = new(
        [], [], [], null, [], [],
        null, null, null, null, null, null);

    /// <summary>True si aucun tag, aucun rythme et aucune note n'est renseigné.</summary>
    public bool EstVide =>
        TechniqueTags.Count == 0
        && ComportementaleTags.Count == 0
        && CulturelleTags.Count == 0
        && MotivationnelleTags.Count == 0
        && PointsVigilanceTags.Count == 0
        && RythmeTravail is null
        && string.IsNullOrWhiteSpace(TechniqueNotes)
        && string.IsNullOrWhiteSpace(ComportementaleNotes)
        && string.IsNullOrWhiteSpace(CulturelleNotes)
        && string.IsNullOrWhiteSpace(OrganisationnelleNotes)
        && string.IsNullOrWhiteSpace(MotivationnelleNotes)
        && string.IsNullOrWhiteSpace(PointsVigilanceNotes);
}

/// <summary>
/// Point d'entrée public du module Profil Candidat. Le module Compatibilité passe exclusivement par
/// cette interface — jamais d'accès direct à <c>ProfilCandidatDbContext</c> depuis l'extérieur du module.
/// </summary>
public interface ICandidateProfileService
{
    Task<int> GetOrCreateProfileIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Identifiant profil s'il existe — <c>null</c> sinon, sans créer de ligne.</summary>
    Task<int?> TryGetProfileIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Les 6 thèmes du questionnaire avec, pour chaque question, la réponse déjà donnée par ce candidat le cas échéant.</summary>
    Task<IReadOnlyList<CandidateQuestionView>> GetQuestionnaireAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    Task SaveAnswerAsync(int candidateProfileId, int questionId, string? answerText, CancellationToken cancellationToken = default);

    /// <summary>Régénère la synthèse de profil à partir des réponses actuelles (heuristique simple, voir implémentation).</summary>
    Task<CandidateSynthesisView> GenerateSynthesisAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    Task<CandidateSynthesisView?> GetLastSynthesisAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Coche/décoche UN tag sur UN axe de la grille (mutation ciblée, protégée contre les écritures
    /// concurrentes — voir l'implémentation). <paramref name="field"/> doit être un axe à tags
    /// (pas <see cref="CriteriaField.Organisationnelle"/>, qui n'a qu'un rythme, voir <see cref="SetRythmeTravailAsync"/>).
    /// </summary>
    Task ToggleTagAsync(int candidateProfileId, CriteriaField field, string tag, bool isChecked, CancellationToken cancellationToken = default);

    Task SetRythmeTravailAsync(int candidateProfileId, int? rythme, CancellationToken cancellationToken = default);

    Task SetNotesAsync(int candidateProfileId, CriteriaField field, string? notes, CancellationToken cancellationToken = default);

    /// <summary>Utilisé exclusivement par le Moteur de Compatibilité pour lire les critères déclarés par le candidat.</summary>
    Task<CandidateCompatibilityCriteriaView?> GetCompatibilityCriteriaAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    // ── Formulaire de CV (sections 1 à 8) ───────────────────────────────────
    //
    // Sauvegarde par section (une méthode par bloc logique), jamais un unique gros formulaire soumis d'un
    // coup — cohérent avec le pattern grille H. Chaque section porte un jeton de concurrence optimiste xmin
    // (voir ProfilCandidatDbContext) même si la contention attendue est faible ici (un seul candidat édite
    // son propre CV) : garder le réflexe pour éviter de réintroduire la classe de bug de perte de mise à
    // jour déjà rencontrée deux fois sur ce module (voir MutateCriteriaAsync/MutateAnswerAsync).

    /// <summary>CV complet du candidat — toutes les sections, y compris les listes vides si rien n'a encore été rempli.</summary>
    Task<CvView> GetCvAsync(int candidateProfileId, CancellationToken cancellationToken = default);

    /// <summary>Section 1 (Coordonnées) — une par candidat. <c>input.Id</c>/<c>CandidateProfileId</c> ignorés, résolus côté serveur.</summary>
    Task SaveCoordonneesAsync(int candidateProfileId, CvCoordonnees input, CancellationToken cancellationToken = default);

    /// <summary>Section 2 (Formations) — <paramref name="id"/> null crée une nouvelle ligne, sinon met à jour la ligne existante (scopée au candidat). Retourne l'Id de la ligne.</summary>
    Task<int> SaveFormationAsync(int candidateProfileId, int? id, CvFormation input, CancellationToken cancellationToken = default);

    Task DeleteFormationAsync(int candidateProfileId, int formationId, CancellationToken cancellationToken = default);

    /// <summary>Section 3 (Spécialités et compétences acquises par les études) — une par candidat.</summary>
    Task SaveCompetencesEtudesAsync(int candidateProfileId, CvCompetencesEtudes input, CancellationToken cancellationToken = default);

    /// <summary>Section 4 (Expériences pratiques) — même logique d'upsert que <see cref="SaveFormationAsync"/>.</summary>
    Task<int> SaveExperienceAsync(int candidateProfileId, int? id, CvExperience input, CancellationToken cancellationToken = default);

    Task DeleteExperienceAsync(int candidateProfileId, int experienceId, CancellationToken cancellationToken = default);

    /// <summary>Section 5 (Caractéristiques personnelles) — une par candidat.</summary>
    Task SaveCaracteristiquesPersonnellesAsync(int candidateProfileId, CvCaracteristiquesPersonnelles input, CancellationToken cancellationToken = default);

    /// <summary>Section 6 (Loisirs et centres d'intérêt) — une par candidat.</summary>
    Task SaveLoisirsAsync(int candidateProfileId, CvLoisirs input, CancellationToken cancellationToken = default);

    /// <summary>Section 7 (Références professionnelles) — même logique d'upsert que <see cref="SaveFormationAsync"/>.</summary>
    Task<int> SaveReferenceAsync(int candidateProfileId, int? id, CvReference input, CancellationToken cancellationToken = default);

    Task DeleteReferenceAsync(int candidateProfileId, int referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Section 8 (Déclaration) — une par candidat. <see cref="CvDeclaration.ConsentementConsultation"/> est
    /// capturé et affiché mais ne déclenche AUCUN changement d'accès (voir la remarque sur l'entité) :
    /// jamais branché sur un mécanisme de recherche libre de CV ni sur la visibilité Vivier.
    /// </summary>
    Task SaveDeclarationAsync(int candidateProfileId, CvDeclaration input, CancellationToken cancellationToken = default);
}
