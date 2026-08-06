namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Section 1 du formulaire de CV (« Coordonnées du postulant ») — une par candidat, tous les champs
/// facultatifs : un candidat qui n'a pas encore rempli son CV continue de fonctionner normalement
/// (voir <see cref="Data.ProfilCandidatDbContext"/>, index unique sur <see cref="CandidateProfileId"/>).
/// </summary>
public sealed class CvCoordonnees
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public string? Nom { get; set; }
    public string? Prenoms { get; set; }
    public DateOnly? DateNaissance { get; set; }
    public string? LieuNaissance { get; set; }
    public string? Nationalite { get; set; }
    public string? AdresseComplete { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? ProfilOuPosteRecherche { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
