namespace Spectrometre.Modules.PostesRecrutement.Services;

/// <summary>
/// Données déjà résolues pour la mise en page PDF — pas d'accès DB ici (même contrat que
/// <c>ICvPdfService.GenerateCvPdf(CvView)</c>).
/// </summary>
public sealed record AnalysePdfModel(
    string TitrePoste,
    int CandidateProfileId,
    string? NomCandidat,
    int? ScoreCompatibilite,
    string AnalyseTexte,
    DateTimeOffset GenereeLe,
    bool GenereeParIa,
    bool English = false);

/// <summary>
/// Export PDF du rapport d'analyse IA poste/candidature — même bibliothèque que
/// <c>ICvPdfService</c> (QuestPDF, licence Community).
/// </summary>
public interface IAnalysePdfService
{
    /// <summary>Génère le PDF à partir du modèle DÉJÀ chargé — mise en page uniquement.</summary>
    byte[] GenerateAnalysePdf(AnalysePdfModel model);
}
