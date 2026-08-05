using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.SuiviEvolutif.Data;

namespace Spectrometre.Modules.SuiviEvolutif.Services;

public sealed class SuiviEvolutifService(
    IDbContextFactory<SuiviEvolutifCandidatDbContext> candidatDbFactory,
    IDbContextFactory<SuiviEvolutifEntrepriseDbContext> entrepriseDbFactory,
    ITenantContext tenantContext) : ISuiviEvolutifService
{
    public async Task<IReadOnlyList<HistoriqueEntreeView>> GetHistoriqueCandidatAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await candidatDbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Entries
            .AsNoTracking()
            .Where(e => e.CandidateProfileId == candidateProfileId)
            .OrderByDescending(e => e.Horodatage)
            .Select(e => new HistoriqueEntreeView(LabelPourChamp(e.Champ), e.AncienneValeur, e.NouvelleValeur, e.Horodatage))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistoriqueEntreeView>> GetHistoriqueEntrepriseAsync(int companyProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await entrepriseDbFactory.CreateDbContextAsync(cancellationToken);
        db.TenantSchema = tenantContext.SchemaName;

        try
        {
            return await db.Entries
                .AsNoTracking()
                .Where(e => e.CompanyProfileId == companyProfileId)
                .OrderByDescending(e => e.Horodatage)
                .Select(e => new HistoriqueEntreeView(LabelPourChamp(e.Champ), e.AncienneValeur, e.NouvelleValeur, e.Horodatage))
                .ToListAsync(cancellationToken);
        }
        catch (Npgsql.PostgresException)
        {
            // Schéma pas encore provisionné pour ce tenant (ex. entreprise créée avant l'ajout de ce
            // module et n'ayant jamais activé SuiviEvolutif — voir TenantSchemaSynchronizer, qui ne
            // provisionne que les modules marqués actifs) : pas d'historique à montrer plutôt qu'une erreur.
            return [];
        }
    }

    /// <summary>
    /// Traduit un code de champ technique (ex. <c>"Organisationnelle.Rythme"</c>, voir comment
    /// <c>CandidateProfileService</c>/<c>CompanyProfileService</c> appellent <c>IProfileChangeRecorder</c>)
    /// en libellé lisible pour un profil non-développeur — jamais un nom de colonne affiché tel quel.
    /// </summary>
    private static string LabelPourChamp(string champ) => champ switch
    {
        "Technique.Tags" => "Compétences techniques",
        "Comportementale.Tags" => "Comportements professionnels",
        "Culturelle.Tags" => "Valeurs culturelles",
        "Organisationnelle.Rythme" => "Rythme de travail",
        "Motivationnelle.Tags" => "Sources de motivation",
        "PointsVigilance.Tags" => "Points de vigilance",
        "Technique.Notes" => "Notes — compétences techniques",
        "Comportementale.Notes" => "Notes — comportement",
        "Culturelle.Notes" => "Notes — culture",
        "Organisationnelle.Notes" => "Notes — organisation",
        "Motivationnelle.Notes" => "Notes — motivation",
        "PointsVigilance.Notes" => "Notes — points de vigilance",
        _ => champ,
    };
}
