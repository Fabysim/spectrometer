namespace Spectrometre.Modules.ProfilCandidat.Services;

public sealed class CvImportService(
    ICvDocumentTextExtractor extractor,
    ICvImportIaService importIa) : ICvImportService
{
    public async Task<CvImportResultat> ImporterAsync(
        Stream contenu,
        string fileName,
        string? contentType,
        long tailleOctets,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (tailleOctets > ICvDocumentTextExtractor.TailleMaxOctets)
                return new CvImportResultat(false, "Cv_Import_FichierTropVolumineux", null);

            if (!extractor.EstFormatAccepte(fileName, contentType))
                return new CvImportResultat(false, "Cv_Import_FormatRefuse", null);

            var texte = await extractor.ExtraireAsync(contenu, fileName, cancellationToken);
            if (string.IsNullOrWhiteSpace(texte))
                return new CvImportResultat(false, "Cv_Import_Illisible", null);

            var brouillon = await importIa.ExtraireCvAsync(texte, cancellationToken);
            if (brouillon is null)
                return new CvImportResultat(false, "Cv_Import_IaEchec", null);

            return new CvImportResultat(true, "Cv_Import_PretARelire", brouillon);
        }
        catch
        {
            return new CvImportResultat(false, "Cv_Import_IaEchec", null);
        }
    }
}
