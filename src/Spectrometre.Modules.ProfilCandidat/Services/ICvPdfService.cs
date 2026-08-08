namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Export PDF du CV structuré (sections 1 à 8) — même bibliothèque que <c>mvp</c>
/// (<c>Spectrometre.Services.PdfReportGenerator</c>, QuestPDF, licence Community) plutôt que d'en introduire
/// une nouvelle. Respecte simplement l'ordre et le contenu des sections telles que saisies — pas de mise en
/// page élaborée, la lisibilité prime sur le design (voir la demande d'origine).
/// </summary>
public interface ICvPdfService
{
    /// <summary>Génère le PDF pour le CV DÉJÀ chargé (voir <c>ICandidateProfileService.GetCvAsync</c>) — pas de nouvel accès aux données ici, cette méthode ne fait que mettre en page.</summary>
    byte[] GenerateCvPdf(CvView cv);
}
