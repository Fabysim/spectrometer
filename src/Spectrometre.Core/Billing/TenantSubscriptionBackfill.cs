using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;

namespace Spectrometre.Core.Billing;

/// <summary>
/// Comble rétroactivement l'abonnement des entreprises créées AVANT l'exigence d'un abonnement pour
/// l'accès effectif aux modules — sans ce backfill, ces entreprises perdraient l'accès à tous leurs
/// modules déjà activés dès le démarrage (voir <c>ModuleRegistry.IsActiveAsync</c> : échec fermé si aucun
/// abonnement Essai/Active). Assigne le libellé <see cref="PlanCodes.Standard"/> (étiquette informative).
/// Idempotent : une entreprise déjà abonnée est ignorée.
/// </summary>
/// <remarks>
/// Vit dans le noyau (pas dans <c>Spectrometre.Host</c>) car il ne touche que des données <c>core</c>
/// (<c>Companies</c>/<c>TenantSubscriptions</c>) — contrairement à <c>RecruitmentIndexBackfill</c>, qui a
/// besoin des DbContext de modules et doit donc rester dans Host. Ça le rend directement testable depuis
/// le projet de tests sans dépendre de Host.
/// </remarks>
public static class TenantSubscriptionBackfill
{
    public static async Task RunAsync(CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var companyIdsAvecAbonnement = await db.TenantSubscriptions
            .Select(s => s.CompanyId)
            .ToListAsync(cancellationToken);

        var companiesSansAbonnement = await db.Companies
            .Where(c => !companyIdsAvecAbonnement.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (companiesSansAbonnement.Count == 0)
            return;

        foreach (var company in companiesSansAbonnement)
        {
            db.TenantSubscriptions.Add(new TenantSubscription
            {
                CompanyId = company.Id,
                PlanCode = PlanCodes.Standard,
                Status = SubscriptionStatus.Active,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
