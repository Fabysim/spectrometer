using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class FacturationCalculatorTests(ServiceFixture fixture)
{
    [Fact]
    public async Task CalculerMontantDuAsync_SommeUniquementDesModulesFacturablesEffectifs()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var company = await fixture.CreateCompanyAsync($"Carte Co {suffix}", $"carte-owner-{suffix}");

        await using var scope = fixture.Services.CreateAsyncScope();
        var core = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var registry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var calc = scope.ServiceProvider.GetRequiredService<IFacturationCalculatorService>();

        // CreateCompanyAsync active déjà plusieurs add-ons Matching Emploi ; on ajoute GDT + SuiviEmployes.
        await registry.SetActiveAsync(ModuleActivationSubjectType.Company, company.Id, "GestionDuTemps", true, core);
        await registry.SetActiveAsync(ModuleActivationSubjectType.Company, company.Id, "SuiviEmployes", true, core);

        // Statut Active pour que les modules activés soient EFFECTIFS (IsActiveAsync).
        var sub = await core.TenantSubscriptions.FirstAsync(s => s.CompanyId == company.Id);
        sub.Status = SubscriptionStatus.Active;
        await core.SaveChangesAsync();

        var gdt = await core.ModulePrix.AsNoTracking().FirstAsync(p => p.ModuleCode == "GestionDuTemps");
        var se = await core.ModulePrix.AsNoTracking().FirstAsync(p => p.ModuleCode == "SuiviEmployes");
        Assert.True(gdt.Facturable && se.Facturable);

        var avant = await calc.CalculerMontantDuAsync(ModuleActivationSubjectType.Company, company.Id, core);
        Assert.Contains(avant.Lignes, l => l.ModuleCode == "GestionDuTemps" && l.PrixMensuel == gdt.PrixMensuel);
        Assert.Contains(avant.Lignes, l => l.ModuleCode == "SuiviEmployes" && l.PrixMensuel == se.PrixMensuel);
        Assert.DoesNotContain(avant.Lignes, l => l.ModuleCode == "ProfilEntreprise");
        Assert.Equal(avant.Lignes.Sum(l => l.PrixMensuel), avant.Total);
        Assert.True(avant.Total >= gdt.PrixMensuel + se.PrixMensuel);

        await registry.SetActiveAsync(ModuleActivationSubjectType.Company, company.Id, "GestionDuTemps", false, core);

        var apres = await calc.CalculerMontantDuAsync(ModuleActivationSubjectType.Company, company.Id, core);
        Assert.DoesNotContain(apres.Lignes, l => l.ModuleCode == "GestionDuTemps");
        Assert.Contains(apres.Lignes, l => l.ModuleCode == "SuiviEmployes");
        Assert.Equal(avant.Total - gdt.PrixMensuel, apres.Total);
    }
}
