using Microsoft.Extensions.Localization;
using Spectrometre.Modules.JeunesPrestataires.Resources;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>
/// PDF du consentement parental validé — même bibliothèque et licence Community
/// que <c>IChartePdfService</c> / <c>ICvPdfService</c> (QuestPDF), pas de nouvelle dépendance.
/// </summary>
public interface IConsentementParentalPdfService
{
    byte[] GeneratePdf(
        JeuneProfileView jeune,
        ConsentementParentalView consentement,
        IStringLocalizer<JeunesPrestatairesResource> localizer);
}
