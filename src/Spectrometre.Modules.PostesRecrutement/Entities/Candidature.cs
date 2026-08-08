namespace Spectrometre.Modules.PostesRecrutement.Entities;

public enum CandidatureStatut
{
    Recue = 0,
    EnRevue = 1,
    Entretien = 2,
    Rejetee = 3,
    Embauchee = 4,
}

/// <summary>Libellés d'affichage du statut (bilinguisme, cycle contenu métier) — le statut lui-même reste un enum stocké tel quel (et la chaîne partagée avec l'index Analytics via <c>CandidatureIndexEntry.Statut</c>), ces libellés ne sont utilisés qu'à l'affichage.</summary>
public static class CandidatureStatutLabels
{
    public static string Label(CandidatureStatut statut, bool english) => statut switch
    {
        CandidatureStatut.Recue => english ? "Received" : "Reçue",
        CandidatureStatut.EnRevue => english ? "Under review" : "En revue",
        CandidatureStatut.Entretien => english ? "Interview" : "Entretien",
        CandidatureStatut.Rejetee => english ? "Rejected" : "Rejetée",
        CandidatureStatut.Embauchee => english ? "Hired" : "Embauchée",
        _ => statut.ToString(),
    };
}

/// <summary>
/// Candidature d'un candidat à un poste. Vit dans le schéma de l'entreprise qui a publié le poste
/// (donnée de recrutement de cette entreprise). <see cref="CandidateProfileId"/> est une référence par
/// identifiant vers le module Profil Candidat (schéma fixe, non tenant-scopé) — jamais une contrainte de
/// clé étrangère inter-schéma, même principe que <c>CompatibilityResult.CandidateProfileId</c>.
/// </summary>
public sealed class Candidature
{
    public int Id { get; set; }
    public int PosteId { get; set; }
    public int CandidateProfileId { get; set; }
    public CandidatureStatut Statut { get; set; } = CandidatureStatut.Recue;

    /// <summary>Équivalent de <c>PositionCandidate.IsSelected</c> du MVP — shortlist avant décision finale.</summary>
    public bool EstPreselectionne { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Niveaux finaux ajustés par l'entreprise (une ligne par critère du poste).</summary>
    public ICollection<EvaluationCritereCandidature> EvaluationsFinales { get; set; } = [];
}
