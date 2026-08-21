using System.Globalization;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Spectrometre.Modules.JeunesPrestataires.Resources;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>Mise en page du consentement — même approche que <see cref="ChartePdfService"/>.</summary>
public sealed class ConsentementParentalPdfService : IConsentementParentalPdfService
{
    static ConsentementParentalPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(
        JeuneProfileView jeune,
        ConsentementParentalView consentement,
        IStringLocalizer<JeunesPrestatairesResource> localizer)
    {
        var entity = consentement.Entity;
        var culture = CultureInfo.CurrentCulture;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).LineHeight(1.3f));

                page.Header().Column(header =>
                {
                    header.Item().Text(localizer["PageHeading"].Value).FontSize(16).Bold();
                });

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Element(c => Section(c, localizer["Section1_Title"].Value, section =>
                    {
                        Field(section, localizer["Nom"].Value, jeune.Nom);
                        Field(section, localizer["Prenoms"].Value, jeune.Prenoms);
                        Field(section, localizer["DateNaissance"].Value, jeune.DateNaissance.ToString("d", culture));
                        Field(section, localizer["Age"].Value, string.Format(culture, localizer["AgeAns"].Value, CalculerAge(jeune.DateNaissance)));
                    }));

                    column.Item().Element(c => Section(c, localizer["Section2_Title"].Value, section =>
                    {
                        section.Item().PaddingBottom(4).Text(localizer["Parent1_Title"].Value).SemiBold();
                        EcrireRepresentant(section, localizer, entity.Parent1Nom, entity.Parent1Lien, entity.Parent1Adresse, entity.Parent1Telephone, entity.Parent1Email);

                        if (!string.IsNullOrWhiteSpace(entity.Parent2Nom))
                        {
                            section.Item().PaddingTop(8).PaddingBottom(4).Text(localizer["Parent2_Title"].Value).SemiBold();
                            EcrireRepresentant(section, localizer, entity.Parent2Nom, entity.Parent2Lien, entity.Parent2Adresse, entity.Parent2Telephone, entity.Parent2Email);
                        }
                    }));

                    column.Item().Element(c => Section(c, localizer["Section3_Title"].Value, section =>
                    {
                        if (entity.AutorisationMissions)
                            Coche(section, localizer["AutorisationMissions_Label"].Value);
                    }));

                    column.Item().Element(c => Section(c, localizer["Section4_Title"].Value, section =>
                    {
                        if (entity.AutorisationRevenus)
                            Coche(section, localizer["AutorisationRevenus_Label"].Value);
                        Field(section, localizer["PartParascolairePourcent"].Value, FormaterPourcent(entity.PartParascolairePourcent, culture));
                        Field(section, localizer["PartArgentDePochePourcent"].Value, FormaterPourcent(entity.PartArgentDePochePourcent, culture));
                        Field(section, localizer["AutreAffectation"].Value, entity.AutreAffectation);
                        Field(section, localizer["ModalitesVersement"].Value, entity.ModalitesVersement);
                    }));

                    column.Item().Element(c => Section(c, localizer["Section5_Title"].Value, section =>
                    {
                        if (entity.AutorisationDonneesEtImage)
                            Coche(section, localizer["AutorisationDonneesEtImage_Label"].Value);
                    }));

                    column.Item().Element(c => Section(c, localizer["Section6_Title"].Value, section =>
                    {
                        if (entity.EngagementScolariteSanteEquilibre)
                            Coche(section, localizer["EngagementScolariteSanteEquilibre"].Value);
                        if (entity.EngagementInformerContraintes)
                            Coche(section, localizer["EngagementInformerContraintes"].Value);
                        if (entity.EngagementEncouragerCharte)
                            Coche(section, localizer["EngagementEncouragerCharte"].Value);
                        if (entity.EngagementSignalerMissionInadaptee)
                            Coche(section, localizer["EngagementSignalerMissionInadaptee"].Value);
                        if (entity.EngagementCollaborerCoach)
                            Coche(section, localizer["EngagementCollaborerCoach"].Value);
                    }));

                    column.Item().Element(c => Section(c, localizer["Section7_Title"].Value, section =>
                    {
                        Field(section, localizer["NomJeuneConfirmation"].Value, entity.NomJeuneConfirmation);
                        Field(section, localizer["NomParent1Confirmation"].Value, entity.NomParent1Confirmation);
                        Field(section, localizer["NomParent2Confirmation"].Value, entity.NomParent2Confirmation);
                        if (entity.ValideLe is { } valideLe)
                        {
                            Field(
                                section,
                                localizer["Consentement_DateValidation"].Value,
                                valideLe.ToLocalTime().ToString("g", culture));
                        }
                    }));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span(localizer["Consentement_PdfPied"].Value);
                    text.Span("  ·  ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void EcrireRepresentant(
        ColumnDescriptor section,
        IStringLocalizer<JeunesPrestatairesResource> localizer,
        string? nom,
        string? lien,
        string? adresse,
        string? telephone,
        string? email)
    {
        Field(section, localizer["Parent_Nom"].Value, nom);
        Field(section, localizer["Parent_Lien"].Value, lien);
        Field(section, localizer["Parent_Adresse"].Value, adresse);
        Field(section, localizer["Parent_Telephone"].Value, telephone);
        Field(section, localizer["Parent_Email"].Value, email);
    }

    private static void Section(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(12).Bold();
            column.Item().PaddingTop(4).Column(content);
        });
    }

    private static void Field(ColumnDescriptor section, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        section.Item().Text(t =>
        {
            t.Span($"{label} : ").SemiBold();
            t.Span(value);
        });
    }

    private static void Coche(ColumnDescriptor section, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return;
        section.Item().Text("• " + label);
    }

    private static string? FormaterPourcent(decimal? valeur, CultureInfo culture) =>
        valeur is { } v ? v.ToString("0.##", culture) : null;

    private static int CalculerAge(DateOnly dateNaissance)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateNaissance.Year;
        if (dateNaissance > today.AddYears(-age))
            age--;
        return age;
    }
}
