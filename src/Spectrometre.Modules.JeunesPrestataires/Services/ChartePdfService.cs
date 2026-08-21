using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Resources;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>Mise en page simple de la charte — même approche que <see cref="Spectrometre.Modules.ProfilCandidat.Services.CvPdfService"/>.</summary>
public sealed class ChartePdfService : IChartePdfService
{
    static ChartePdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(IStringLocalizer<JeunesPrestatairesResource> localizer)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).LineHeight(1.3f));

                page.Header().Column(header =>
                {
                    header.Item().Text(localizer["Charte_Heading"].Value).FontSize(16).Bold();
                });

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text(localizer["Charte_Intro"].Value);
                    column.Item().Text(localizer["Charte_Intro2"].Value);

                    foreach (var section in CharteCatalog.Sections)
                    {
                        column.Item().Element(c =>
                        {
                            c.Column(sec =>
                            {
                                sec.Item().Text(localizer[$"Charte_S_{section.Key}_Titre"].Value).FontSize(12).Bold();
                                sec.Item().PaddingTop(4).Text(localizer[$"Charte_S_{section.Key}_Corps"].Value);
                            });
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(localizer["Charte_PdfPied"].Value);
                    text.Span("  ·  ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
