using Spectrometre.Modules.Missions.Catalog;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

public sealed class MesAstucesCatalogTests
{
    [Fact]
    public void Fiches_ClesDocumentBouchra_PlusComplements()
    {
        var keys = MesAstucesCatalog.Fiches.Select(f => f.Key).ToList();
        Assert.Equal(7, keys.Count);
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("arriver_a_lheure", keys);
        Assert.Contains("dire_bonjour", keys);
        Assert.Contains("prevenir_probleme", keys);
        Assert.Contains("demander_aide", keys);
        Assert.Contains("se_presenter", keys);
        Assert.Contains("en_retard", keys);
        Assert.Contains("finir_mission", keys);
    }
}
