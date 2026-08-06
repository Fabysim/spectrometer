using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Modules.GestionDuTemps.Entities;
using Spectrometre.Modules.GestionDuTemps.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Profil psychosocial, réflexion consciente et synthèse IA — <see cref="FakeAiSynthesisService"/> substitue
/// l'implémentation Replicate réelle (voir <c>ServiceFixture</c>) : aucun test ici ne touche le réseau.
/// </summary>
[Collection("Base de données partagée")]
public sealed class GestionDuTempsSyntheseTests(ServiceFixture fixture)
{
    private const string ReponseIaValide = """
        {
          "profilType": "Structuré",
          "profilTexte": "Vous maintenez un bon équilibre entre vos engagements professionnels et personnels.",
          "indiceCommentaire": "Votre équilibre global est satisfaisant.",
          "maturiteCommentaire": "Votre organisation est solide.",
          "recommandations": [
            { "priorite": 1, "texte": "Maintenez vos rituels quotidiens.", "domaine": "organisation" }
          ],
          "alertes": []
        }
        """;

    private FakeAiSynthesisService Fake => (FakeAiSynthesisService)fixture.Services.GetRequiredService<IAiSynthesisService>();

    [Fact]
    public async Task SaveProfilPsychosocialAsync_PuisGet_RetourneLesChampsEnregistresScopesAuCycleActif()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-profil-{Guid.NewGuid()}";

        Assert.Null(await service.GetProfilPsychosocialAsync(userId));

        await service.SaveProfilPsychosocialAsync(userId, new ProfilPsychosocial
        {
            UserId = "ignore-moi", // doit être écrasé côté serveur
            CycleId = 999999,      // doit être écrasé côté serveur
            SommeilReparateur = "Souvent",
            ToleranceImprevu = "Adaptatif",
            UtiliseAgenda = true,
            Desequilibres = ["Trop de temps professionnel", "Trop de temps professionnel"], // doublon volontaire
        });

        var profil = await service.GetProfilPsychosocialAsync(userId);
        Assert.NotNull(profil);
        Assert.Equal(userId, profil!.UserId);
        Assert.NotEqual(999999, profil.CycleId);
        Assert.Equal("Souvent", profil.SommeilReparateur);
        Assert.Equal("Adaptatif", profil.ToleranceImprevu);
        Assert.True(profil.UtiliseAgenda);
        Assert.Equal(["Trop de temps professionnel"], profil.Desequilibres); // dédupliqué

        // Un second enregistrement met à jour la même ligne (upsert), pas une nouvelle.
        await service.SaveProfilPsychosocialAsync(userId, new ProfilPsychosocial { UserId = userId, SommeilReparateur = "Rarement" });
        var profilMisAJour = await service.GetProfilPsychosocialAsync(userId);
        Assert.Equal(profil.Id, profilMisAJour!.Id);
        Assert.Equal("Rarement", profilMisAJour.SommeilReparateur);
    }

    [Fact]
    public async Task SaveReflexionConscienteAsync_PuisGet_RetourneLesChampsEnregistres()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-reflexion-{Guid.NewGuid()}";

        Assert.Null(await service.GetReflexionConscienteAsync(userId));

        await service.SaveReflexionConscienteAsync(userId, new ReflexionConsciente
        {
            UserId = "ignore-moi",
            SituationActuelle = "Beaucoup de réunions cette semaine.",
            SourceIdentifiee = true,
            Ressentis = ["Fatigue", "Motivation"],
        });

        var reflexion = await service.GetReflexionConscienteAsync(userId);
        Assert.NotNull(reflexion);
        Assert.Equal(userId, reflexion!.UserId);
        Assert.Equal("Beaucoup de réunions cette semaine.", reflexion.SituationActuelle);
        Assert.True(reflexion.SourceIdentifiee);
        Assert.Equal(["Fatigue", "Motivation"], reflexion.Ressentis);
    }

    [Fact]
    public async Task GenererSyntheseAsync_SansProfilRempli_RetourneUnReplLocalSansAppelerLIa()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-synthese-sans-profil-{Guid.NewGuid()}";

        // Piège volontaire : même si le double IA était configuré pour réussir, l'absence de profil doit
        // court-circuiter l'appel IA (voir GenererSyntheseAsync : "profil is null" → repli direct).
        Fake.Reponse = ReponseIaValide;
        Fake.Erreur = null;

        var synthese = await service.GenererSyntheseAsync(userId);

        Assert.False(synthese.GenereeParIa);
        Assert.Equal("Réactif", synthese.ProfilType);
    }

    [Fact]
    public async Task GenererSyntheseAsync_AvecIaDisponible_ParseLaReponseEtMarqueGenereeParIa()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-synthese-ia-ok-{Guid.NewGuid()}";

        await service.SaveProfilPsychosocialAsync(userId, new ProfilPsychosocial { UserId = userId, UtiliseAgenda = true, RituelsQuotidiens = true });

        Fake.Erreur = null;
        Fake.Reponse = ReponseIaValide;

        var synthese = await service.GenererSyntheseAsync(userId);

        Assert.True(synthese.GenereeParIa);
        Assert.Equal("Structuré", synthese.ProfilType);
        Assert.Equal("Vous maintenez un bon équilibre entre vos engagements professionnels et personnels.", synthese.ProfilTexte);
        var reco = Assert.Single(synthese.Recommandations);
        Assert.Equal("organisation", reco.Domaine);
    }

    [Fact]
    public async Task GenererSyntheseAsync_QuandLIaEchoueOuNestPasConfiguree_RetombeSurUnTexteLocalSansErreur()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-synthese-ia-ko-{Guid.NewGuid()}";

        await service.SaveProfilPsychosocialAsync(userId, new ProfilPsychosocial { UserId = userId, ToleranceImprevu = "Anxieux" });

        // Simule une clé API absente/un échec réseau — jamais d'exception ne doit remonter.
        Fake.Erreur = "Clé API Replicate non configurée.";
        Fake.Reponse = null;

        var synthese = await service.GenererSyntheseAsync(userId);

        Assert.False(synthese.GenereeParIa);
        Assert.NotNull(synthese.ProfilTexte);
        Assert.NotEmpty(synthese.Recommandations);
    }

    [Fact]
    public async Task GenererSyntheseAsync_ReponseIaMalFormee_RetombeAussiSurLeTexteLocal()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-synthese-ia-malformee-{Guid.NewGuid()}";

        await service.SaveProfilPsychosocialAsync(userId, new ProfilPsychosocial { UserId = userId });

        Fake.Erreur = null;
        Fake.Reponse = "ceci n'est pas du JSON valide";

        var synthese = await service.GenererSyntheseAsync(userId);

        Assert.False(synthese.GenereeParIa);
    }

    [Fact]
    public async Task GenererSyntheseAsync_AppeleDeuxFoisSansChangementDeProfil_UtiliseLeCacheEtNeRappelePasLIa()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-synthese-cache-{Guid.NewGuid()}";

        await service.SaveProfilPsychosocialAsync(userId, new ProfilPsychosocial { UserId = userId, UtiliseAgenda = true });

        Fake.Erreur = null;
        Fake.Reponse = ReponseIaValide;
        var premiere = await service.GenererSyntheseAsync(userId);
        Assert.True(premiere.GenereeParIa);

        // Si le hash du profil n'a pas changé, GenererSyntheseAsync doit retourner le résultat en cache SANS
        // consulter à nouveau le service IA — en configurant le double pour échouer, on vérifie qu'il n'est
        // pas sollicité une seconde fois (sinon on retomberait sur GenereeParIa == false).
        Fake.Erreur = "ne devrait jamais être atteint";
        Fake.Reponse = null;
        var seconde = await service.GenererSyntheseAsync(userId);

        Assert.True(seconde.GenereeParIa);
        // Comparaison à la milliseconde près : Postgres stocke un timestamptz à la microseconde, .NET des
        // ticks à 100ns — un aller-retour DB peut arrondir les derniers chiffres sans que cela signifie un
        // second calcul (déjà exclu ci-dessus par la config du double IA configurée pour échouer).
        Assert.Equal(premiere.CalculatedAt.ToUnixTimeMilliseconds(), seconde.CalculatedAt.ToUnixTimeMilliseconds());
        Assert.Equal(premiere.ProfilTexte, seconde.ProfilTexte);

        // En revanche, un changement réel du profil invalide le cache et redéclenche un appel IA.
        await service.SaveProfilPsychosocialAsync(userId, new ProfilPsychosocial { UserId = userId, UtiliseAgenda = true, RituelsQuotidiens = true });
        Fake.Erreur = null;
        Fake.Reponse = ReponseIaValide;
        var troisieme = await service.GenererSyntheseAsync(userId);
        Assert.True(troisieme.GenereeParIa);
        Assert.NotEqual(premiere.CalculatedAt, troisieme.CalculatedAt);
    }
}
