namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Section 8 du formulaire de CV (« Déclaration du postulant ») — une par candidat.
/// <see cref="ConsentementConsultation"/> est capturé et affiché mais ne déclenche AUCUN changement d'accès :
/// la visibilité du profil auprès des entreprises reste régie exclusivement par la règle déjà en place dans
/// Vivier (candidat visible seulement s'il a une candidature réelle déposée sur un poste). Ne jamais brancher
/// ce champ sur un mécanisme de recherche libre de CV — décision actée pour ce cycle.
/// </summary>
public sealed class CvDeclaration
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public bool CertificationExactitude { get; set; }
    public bool ConsentementConsultation { get; set; }
    public DateOnly? Date { get; set; }

    /// <summary>Nom tapé au clavier, jamais une image ou une signature électronique — voir le document source.</summary>
    public string? NomSignataire { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
