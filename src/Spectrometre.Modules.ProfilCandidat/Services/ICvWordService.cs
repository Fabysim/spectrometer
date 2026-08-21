namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Export Word (.docx) du CV structuré (sections 1 à 8) — même contrat que <see cref="ICvPdfService"/> :
/// le CV est DÉJÀ chargé, cette méthode ne fait que la mise en page. Réutilise
/// <c>DocumentFormat.OpenXml</c> déjà présent pour l'extraction à l'import (aucune seconde dépendance).
/// Même ordre de sections que le PDF ; la lisibilité prime sur une mise en forme élaborée.
/// </summary>
public interface ICvWordService
{
    /// <summary>Génère le .docx pour le CV DÉJÀ chargé (voir <c>ICandidateProfileService.GetCvAsync</c>) — pas de nouvel accès aux données ici.</summary>
    byte[] GenerateCvWord(CvView cv);
}
