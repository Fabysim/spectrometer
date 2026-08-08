using Spectrometre.Modules.ProfilEntreprise.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Services;
using Spectrometre.Modules.Recrutement.Data;
using Spectrometre.Modules.Recrutement.Services;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Non-régression de la faille de confidentialité sur <c>/compatibilite/resultat/{id}</c> : avant ce
/// correctif, n'importe quel utilisateur authentifié pouvait consulter le résultat de compatibilité de
/// n'importe quel candidat en changeant l'identifiant dans l'URL, sans qu'une candidature réelle
/// n'existe. Couvre les 4 cas de la règle d'accès sur
/// <c>CompatibiliteService.GetResultatAutorisePourUtilisateurAsync</c>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class CompatibiliteAccessControlTests(ServiceFixture fixture)
{
    /// <summary>Scope frais par appel : chaque test doit voir un <see cref="ITenantContext"/> et un <see cref="CoreDbContext"/> neufs, comme un circuit Blazor différent — pas l'état laissé par un test précédent.</summary>
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    private static async Task<int> GetOrCreateCandidateAsync(IServiceScope scope, string userId) =>
        await scope.ServiceProvider.GetRequiredService<ICandidateProfileService>().GetOrCreateProfileIdAsync(userId);

    /// <summary>Crée un poste dans le tenant de <paramref name="company"/> et fait postuler <paramref name="candidateProfileId"/>.</summary>
    private async Task PostulerAsync(Company company, int candidateProfileId)
    {
        using var scope = NewScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);

        var posteService = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var posteId = await posteService.CreatePosteAsync($"Poste de test {Guid.NewGuid()}", null, null);
        await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);
    }

    [Fact]
    public async Task LeCandidatConsulteSonPropreResultat_EstAutorise()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"access-test-candidat-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Access {suffix}", $"access-test-manager-{suffix}");

        using var setupScope = NewScope();
        var candidateProfileId = await GetOrCreateCandidateAsync(setupScope, candidatUserId);
        await PostulerAsync(company, candidateProfileId);

        using var scope = NewScope();
        // Un candidat n'a normalement pas d'« entreprise active » — voir le commentaire dans
        // CompatibiliteService : un résultat est toujours relatif à UNE entreprise, donc on simule ici le
        // seul contexte où « voir son propre résultat » a un sens (ex. lien reçu depuis cette entreprise).
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetActiveCompany(company.Id, company.SchemaName);
        var compatibiliteService = scope.ServiceProvider.GetRequiredService<ICompatibiliteService>();

        var result = await compatibiliteService.GetResultatAutorisePourUtilisateurAsync(candidateProfileId, candidatUserId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task LeManagerConsulteUnCandidatAyantReellementPostule_EstAutorise()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"access-test-candidat-{suffix}";
        var employeUserId = $"access-test-manager-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Access {suffix}", employeUserId);

        using var setupScope = NewScope();
        var candidateProfileId = await GetOrCreateCandidateAsync(setupScope, candidatUserId);
        await PostulerAsync(company, candidateProfileId);

        using var scope = NewScope();
        var compatibiliteService = scope.ServiceProvider.GetRequiredService<ICompatibiliteService>();

        // Pas d'entreprise active préconfigurée : la résolution doit se faire en cherchant parmi les
        // entreprises gérées par ce manager, exactement comme un accès direct par URL sans navigation préalable.
        var result = await compatibiliteService.GetResultatAutorisePourUtilisateurAsync(candidateProfileId, employeUserId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task LeManagerConsulteUnCandidatNAyantJamaisPostule_EstRefuse()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"access-test-candidat-jamais-{suffix}";
        var employeUserId = $"access-test-manager-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Access {suffix}", employeUserId);

        using var setupScope = NewScope();
        // Le candidat existe (a un profil), mais n'a JAMAIS postulé nulle part — exactement le scénario
        // de la faille d'origine (incrémenter l'identifiant dans l'URL).
        var candidateProfileId = await GetOrCreateCandidateAsync(setupScope, candidatUserId);

        using var scope = NewScope();
        var compatibiliteService = scope.ServiceProvider.GetRequiredService<ICompatibiliteService>();

        var result = await compatibiliteService.GetResultatAutorisePourUtilisateurAsync(candidateProfileId, employeUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UnTiersSansAucunLien_EstRefuse()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"access-test-candidat-{suffix}";
        var employeUserId = $"access-test-manager-{suffix}";
        var tiersUserId = $"access-test-tiers-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Access {suffix}", employeUserId);

        using var setupScope = NewScope();
        var candidateProfileId = await GetOrCreateCandidateAsync(setupScope, candidatUserId);
        await PostulerAsync(company, candidateProfileId);

        using var scope = NewScope();
        var compatibiliteService = scope.ServiceProvider.GetRequiredService<ICompatibiliteService>();

        // tiersUserId n'est ni le candidat, ni gestionnaire d'aucune entreprise — un simple utilisateur
        // authentifié qui a deviné/incrémenté un identifiant dans l'URL.
        var result = await compatibiliteService.GetResultatAutorisePourUtilisateurAsync(candidateProfileId, tiersUserId);

        Assert.Null(result);
    }
}
