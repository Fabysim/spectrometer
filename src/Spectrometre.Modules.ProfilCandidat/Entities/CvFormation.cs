namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Section 2 du formulaire de CV (« Études réussies et diplômes obtenus ») — une ligne par formation,
/// plusieurs par candidat. <see cref="Periode"/> reste un texte libre (le document source ne force aucun
/// format de date, ex. « De 2015 à 2018 »).
/// </summary>
public sealed class CvFormation
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public string? Periode { get; set; }
    public string? Etablissement { get; set; }
    public string? DiplomeCertificatOuNiveau { get; set; }
    public string? DomaineEtudes { get; set; }

    /// <summary>Préserve l'ordre de saisie (aucune réorganisation manuelle prévue dans ce cycle).</summary>
    public int DisplayOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
