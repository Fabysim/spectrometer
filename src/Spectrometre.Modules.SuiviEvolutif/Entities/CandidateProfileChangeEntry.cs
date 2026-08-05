namespace Spectrometre.Modules.SuiviEvolutif.Entities;

/// <summary>
/// Une ligne d'historique pour un candidat — schéma fixe, comme <c>ProfilCandidat</c> (un candidat n'est
/// pas une entreprise, son historique doit rester accessible quelle que soit l'entreprise active).
/// </summary>
public sealed class CandidateProfileChangeEntry
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    /// <summary>Code de champ technique (ex. "Technique.Tags") — traduit en langage courant par la page d'affichage, jamais montré tel quel.</summary>
    public required string Champ { get; set; }

    public string? AncienneValeur { get; set; }
    public string? NouvelleValeur { get; set; }
    public DateTimeOffset Horodatage { get; set; }
}
