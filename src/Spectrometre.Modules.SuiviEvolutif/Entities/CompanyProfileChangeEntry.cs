namespace Spectrometre.Modules.SuiviEvolutif.Entities;

/// <summary>Une ligne d'historique pour le profil d'une entreprise — schéma tenant, comme <c>ProfilEntreprise</c>.</summary>
public sealed class CompanyProfileChangeEntry
{
    public int Id { get; set; }
    public int CompanyProfileId { get; set; }

    /// <summary>Code de champ technique (ex. "Organisationnelle.Rythme") — traduit en langage courant par la page d'affichage, jamais montré tel quel.</summary>
    public required string Champ { get; set; }

    public string? AncienneValeur { get; set; }
    public string? NouvelleValeur { get; set; }
    public DateTimeOffset Horodatage { get; set; }
}
