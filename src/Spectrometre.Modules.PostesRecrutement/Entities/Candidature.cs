namespace Spectrometre.Modules.PostesRecrutement.Entities;

public enum CandidatureStatut
{
    Recue = 0,
    EnRevue = 1,
    Entretien = 2,
    Rejetee = 3,
    Embauchee = 4,
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
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
