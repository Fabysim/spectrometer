using System.Globalization;
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

        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
        return await db.Entries
            .AsNoTracking()
            .Where(e => e.CandidateProfileId == candidateProfileId)
            .OrderByDescending(e => e.Horodatage)
            .Select(e => new HistoriqueEntreeView(LabelPourChamp(e.Champ, english), e.AncienneValeur, e.NouvelleValeur, e.Horodatage))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistoriqueEntreeView>> GetHistoriqueEntrepriseAsync(int companyProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await entrepriseDbFactory.CreateDbContextAsync(cancellationToken);
        db.TenantSchema = tenantContext.SchemaName;

        try
        {
            var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
            return await db.Entries
                .AsNoTracking()
                .Where(e => e.CompanyProfileId == companyProfileId)
                .OrderByDescending(e => e.Horodatage)
                .Select(e => new HistoriqueEntreeView(LabelPourChamp(e.Champ, english), e.AncienneValeur, e.NouvelleValeur, e.Horodatage))
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
    /// Bilinguisme (cycle contenu métier) : libellé choisi selon la culture courante, jamais
    /// <c>AncienneValeur</c>/<c>NouvelleValeur</c>, qui restent les valeurs historisées telles quelles.
    /// </summary>
    private static string LabelPourChamp(string champ, bool english) => (champ, english) switch
    {
        ("Technique.Tags", false) => "Compétences techniques",
        ("Technique.Tags", true) => "Technical skills",
        ("Comportementale.Tags", false) => "Comportements professionnels",
        ("Comportementale.Tags", true) => "Professional behaviors",
        ("Culturelle.Tags", false) => "Valeurs culturelles",
        ("Culturelle.Tags", true) => "Cultural values",
        ("Organisationnelle.Rythme", false) => "Rythme de travail",
        ("Organisationnelle.Rythme", true) => "Work pace",
        ("Motivationnelle.Tags", false) => "Sources de motivation",
        ("Motivationnelle.Tags", true) => "Sources of motivation",
        ("PointsVigilance.Tags", false) => "Points de vigilance",
        ("PointsVigilance.Tags", true) => "Points of caution",
        ("Technique.Notes", false) => "Notes — compétences techniques",
        ("Technique.Notes", true) => "Notes — technical skills",
        ("Comportementale.Notes", false) => "Notes — comportement",
        ("Comportementale.Notes", true) => "Notes — behavior",
        ("Culturelle.Notes", false) => "Notes — culture",
        ("Culturelle.Notes", true) => "Notes — culture",
        ("Organisationnelle.Notes", false) => "Notes — organisation",
        ("Organisationnelle.Notes", true) => "Notes — organization",
        ("Motivationnelle.Notes", false) => "Notes — motivation",
        ("Motivationnelle.Notes", true) => "Notes — motivation",
        ("PointsVigilance.Notes", false) => "Notes — points de vigilance",
        ("PointsVigilance.Notes", true) => "Notes — points of caution",
        _ => champ,
    };
}
