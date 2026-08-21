using Spectrometre.Modules.JeunesPrestataires.Entities;
using Spectrometre.Modules.Missions.Catalog;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

public sealed class MesAstucesRecommandationsCatalogTests
{
    [Fact]
    public void Starter_SansExperience_TroisFichesDepart()
    {
        var fiches = MesAstucesRecommandationsCatalog.Selectionner(
            aucuneMissionTerminee: true,
            ProfilAccompagnement.SansExperience,
            derniereEval: null,
            dernierScoreCommunication: null,
            dernierScoreAutonomie: null);
        Assert.Equal(
            MesAstucesRecommandationsCatalog.StarterSansExperience,
            fiches.Select(f => f.Key).ToArray());
    }

    [Fact]
    public void Starter_Autonome_JeuDistinct()
    {
        var fiches = MesAstucesRecommandationsCatalog.Selectionner(
            true, ProfilAccompagnement.Autonome, null, null, null);
        Assert.Equal(
            MesAstucesRecommandationsCatalog.StarterAutonome,
            fiches.Select(f => f.Key).ToArray());
    }

    [Fact]
    public void PonctualiteFalse_RecommandeArriverEtRetard()
    {
        var fiches = MesAstucesRecommandationsCatalog.Selectionner(
            false,
            ProfilAccompagnement.SansExperience,
            new MesAstucesEvalSignaux(false, true, true, true),
            dernierScoreCommunication: 5,
            dernierScoreAutonomie: 5);
        Assert.Equal(["arriver_a_lheure", "en_retard"], fiches.Select(f => f.Key).ToArray());
    }

    [Fact]
    public void ScoresOk_AucuneRecommandation()
    {
        var fiches = MesAstucesRecommandationsCatalog.Selectionner(
            false,
            ProfilAccompagnement.SansExperience,
            new MesAstucesEvalSignaux(true, true, true, true),
            5,
            5);
        Assert.Empty(fiches);
    }

    [Fact]
    public void PlafondTrois_PrioriteEvaluationSurGrille()
    {
        var fiches = MesAstucesRecommandationsCatalog.Selectionner(
            false,
            ProfilAccompagnement.SansExperience,
            new MesAstucesEvalSignaux(false, false, false, true),
            dernierScoreCommunication: 2,
            dernierScoreAutonomie: 2);
        Assert.Equal(MesAstucesRecommandationsCatalog.MaxFiches, fiches.Count);
        Assert.Equal(["arriver_a_lheure", "en_retard", "demander_aide"], fiches.Select(f => f.Key).ToArray());
    }
}
