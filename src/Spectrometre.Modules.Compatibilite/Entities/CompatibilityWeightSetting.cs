namespace Spectrometre.Modules.Compatibilite.Entities;

/// <summary>
/// Poids d'un axe dans le calcul du score global, en pourcentage (somme des 5 axes = 100).
/// Table éditable — permet d'ajuster la pondération par entreprise sans redéploiement,
/// comme demandé (« poids par défaut égaux, exposés dans... une table pour ajustement ultérieur »).
/// </summary>
public sealed class CompatibilityWeightSetting
{
    public int Id { get; set; }
    public CompatibilityAxis Axis { get; set; }
    public decimal WeightPercent { get; set; } = 20m;
}
