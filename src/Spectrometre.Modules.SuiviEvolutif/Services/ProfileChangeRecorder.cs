using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Suivi;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.SuiviEvolutif.Data;
using Spectrometre.Modules.SuiviEvolutif.Entities;

namespace Spectrometre.Modules.SuiviEvolutif.Services;

/// <summary>
/// Implémentation réelle de <see cref="IProfileChangeRecorder"/> — voir le commentaire sur l'interface
/// pour l'inversion de dépendance. Enregistrée dans le conteneur DI depuis <c>Spectrometre.Host.Program</c>
/// (par-dessus <c>NoOpProfileChangeRecorder</c>), jamais depuis ProfilCandidat/ProfilEntreprise.
/// </summary>
/// <remarks>
/// Choix de portée : le côté CANDIDAT (schéma fixe, pas de notion d'entreprise) est toujours tracé —
/// l'historique d'un candidat est sa propre donnée, pas un service que « l'entreprise » activerait ou non.
/// Le côté ENTREPRISE, en revanche, est un module comme un autre : tracé uniquement si SuiviEvolutif est
/// activé pour l'entreprise ACTIVE au moment de l'appel (silencieux sinon, même filet de sécurité que
/// <see cref="Spectrometre.Core.Suivi.NoOpProfileChangeRecorder"/>, mais décidé ICI par tenant plutôt que
/// globalement). Un appel sans changement réel (ancienne == nouvelle valeur) n'écrit rien.
/// </remarks>
public sealed class ProfileChangeRecorder(
    IDbContextFactory<SuiviEvolutifCandidatDbContext> candidatDbFactory,
    IDbContextFactory<SuiviEvolutifEntrepriseDbContext> entrepriseDbFactory,
    ITenantContext tenantContext,
    IModuleRegistry moduleRegistry,
    CoreDbContext coreDb) : IProfileChangeRecorder
{
    public async Task RecordChangeAsync(
        int ownerId,
        ProfileOwnerType ownerType,
        string champ,
        string? ancienneValeur,
        string? nouvelleValeur,
        DateTimeOffset horodatage,
        CancellationToken cancellationToken = default)
    {
        if (ancienneValeur == nouvelleValeur)
            return;

        if (ownerType == ProfileOwnerType.Candidat)
        {
            await using var db = await candidatDbFactory.CreateDbContextAsync(cancellationToken);
            db.Entries.Add(new CandidateProfileChangeEntry
            {
                CandidateProfileId = ownerId,
                Champ = champ,
                AncienneValeur = ancienneValeur,
                NouvelleValeur = nouvelleValeur,
                Horodatage = horodatage,
            });
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (tenantContext.ActiveCompanyId is not int companyId)
            return;
        if (!await moduleRegistry.IsActiveAsync(companyId, "SuiviEvolutif", coreDb, cancellationToken))
            return;

        await using var entrepriseDb = await entrepriseDbFactory.CreateDbContextAsync(cancellationToken);
        entrepriseDb.TenantSchema = tenantContext.SchemaName;
        entrepriseDb.Entries.Add(new CompanyProfileChangeEntry
        {
            CompanyProfileId = ownerId,
            Champ = champ,
            AncienneValeur = ancienneValeur,
            NouvelleValeur = nouvelleValeur,
            Horodatage = horodatage,
        });
        await entrepriseDb.SaveChangesAsync(cancellationToken);
    }
}
