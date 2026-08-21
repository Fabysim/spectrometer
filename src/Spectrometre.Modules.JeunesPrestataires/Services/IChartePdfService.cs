using Microsoft.Extensions.Localization;
using Spectrometre.Modules.JeunesPrestataires.Resources;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>
/// PDF de la charte (document de référence générique) — même bibliothèque et licence Community
/// que <c>ICvPdfService</c> / <c>CvPdfService</c> (QuestPDF), pas de nouvelle dépendance.
/// </summary>
public interface IChartePdfService
{
    byte[] GeneratePdf(IStringLocalizer<JeunesPrestatairesResource> localizer);
}
