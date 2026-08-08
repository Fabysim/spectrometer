using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Spectrometre.Core.Ai;
using Spectrometre.Modules.ProfilEntreprise.Entities;

namespace Spectrometre.Modules.ProfilEntreprise.Services;

/// <summary>
/// Offre d'emploi .docx via Claude — structure visuelle alignée sur le MVP
/// (<c>JobOfferDraftService</c>) : bandeau titre, sections MAJUSCULES, puces, pied de page.
/// Couleurs fixes (#1B3A6B / #2E5B8A / #E8EEF7) — pas de OwnerSettings côté modulaire.
/// </summary>
public sealed class JobOfferDraftService(
    IPosteService posteService,
    IReplicateService replicate) : IJobOfferDraftService
{
    private const string ColorPrimary = "1B3A6B";
    private const string ColorSecondary = "2E5B8A";
    private const string ColorBackground = "E8EEF7";

    /// <summary>Message stable pour « poste hors schéma / introuvable » — l'endpoint continue sur l'entreprise suivante.</summary>
    public const string ErreurPosteIntrouvable = "Poste introuvable.";

    public async Task<(byte[]? Content, string? FileName, string? Erreur)> GenererOffreDocxAsync(
        int posteId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var postes = await posteService.GetPostesAsync(cancellationToken);
            var poste = postes.FirstOrDefault(p => p.Id == posteId);
            if (poste is null)
                return (null, null, ErreurPosteIntrouvable);

            var criteres = await posteService.GetCriteresAsync(posteId, cancellationToken);
            var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";

            var systemPrompt = BuildSystemPrompt(english);
            var userPrompt = BuildUserPrompt(poste, criteres, english);

            var (outputText, error) = await replicate.RunClaudeAsync(systemPrompt, userPrompt, cancellationToken);
            if (error is not null)
                return (null, null, error);
            if (string.IsNullOrWhiteSpace(outputText))
                return (null, null, english ? "The model response is empty." : "La réponse du modèle est vide.");

            var docxBytes = BuildDocx(poste.Titre, outputText, english);
            var fileName = english
                ? $"JobOffer_{SanitizeFileName(poste.Titre)}_{DateTime.UtcNow:yyyyMMdd_HHmm}.docx"
                : $"Offre_{SanitizeFileName(poste.Titre)}_{DateTime.UtcNow:yyyyMMdd_HHmm}.docx";
            return (docxBytes, fileName, null);
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

    /// <summary>Construit un .docx à partir d'un texte déjà généré — exposé pour les tests (signature ZIP).</summary>
    public static byte[] BuildDocx(string title, string bodyText, bool english) =>
        BuildDocxInternal(title, bodyText, ColorPrimary, ColorSecondary, ColorBackground, english);

    private static string BuildSystemPrompt(bool english) => english
        ? """
          You are an expert in job offer writing and HR communication. Your role is to write clear, attractive offers that follow best practices.

          ABSOLUTE RULES:
          - Write ONLY in English.
          - Tone: professional, inclusive and precise.
          - Structure: job title, company/role overview, main responsibilities, required profile (criteria and expected levels), conditions (compensation, benefits), closing date and application instructions.
          - Use ONLY the information provided; do not invent any data.
          - For the required profile: list each criterion with the expected level (e.g. "Sense of commitment: high required level").
          - Be concise but complete; one to two pages is ideal for an offer.
          """
        : """
          Tu es un expert en rédaction d'offres d'emploi et en communication RH. Ton rôle est de rédiger des offres claires, attractives et conformes aux bonnes pratiques.

          RÈGLES ABSOLUES :
          - Rédige UNIQUEMENT en français.
          - Ton : professionnel, inclusif et précis.
          - Structure : titre du poste, présentation de l'entreprise/du poste, missions principales, profil recherché (critères et niveaux attendus), conditions (rémunération, avantages), date de clôture et modalités de candidature.
          - Utilise UNIQUEMENT les informations fournies ; n'invente aucune donnée.
          - Pour le profil recherché : liste chaque critère avec le niveau attendu (ex. « Sens de l'engagement : niveau requis élevé »).
          - Sois concis mais complet ; une à deux pages sont idéales pour une offre.
          """;

    private static string BuildUserPrompt(PosteView poste, IReadOnlyList<CritereEvaluationView> criteres, bool english)
    {
        var culture = CultureInfo.CurrentUICulture;
        string Pt(string fr, string en) => english ? en : fr;
        string NotProvided() => english ? "(not provided)" : "(non fourni)";
        string NotProvidedF() => english ? "(not provided)" : "(non fournie)";

        var sb = new StringBuilder();
        sb.AppendLine(Pt(
            "Rédige une offre d'emploi complète à partir des éléments suivants.",
            "Write a complete job offer from the following elements."));
        sb.AppendLine();
        sb.AppendLine($"## {Pt("Intitulé du poste", "Job title")}");
        sb.AppendLine(poste.Titre);
        sb.AppendLine();
        sb.AppendLine($"## {Pt("Description du poste", "Job description")}");
        sb.AppendLine(string.IsNullOrWhiteSpace(poste.Description) ? NotProvided() : poste.Description);
        sb.AppendLine();
        sb.AppendLine($"## {Pt("Description des tâches / missions", "Tasks / responsibilities description")}");
        sb.AppendLine(string.IsNullOrWhiteSpace(poste.TachesDescription) ? NotProvided() : poste.TachesDescription);
        sb.AppendLine();
        sb.AppendLine($"## {Pt("Rémunération", "Compensation")}");
        sb.AppendLine(string.IsNullOrWhiteSpace(poste.Salaire) ? NotProvided() : poste.Salaire);
        sb.AppendLine();
        sb.AppendLine($"## {Pt("Avantages sociaux", "Benefits")}");
        sb.AppendLine(string.IsNullOrWhiteSpace(poste.Avantages) ? NotProvided() : poste.Avantages);
        sb.AppendLine();
        sb.AppendLine($"## {Pt("Date de clôture des candidatures", "Application closing date")}");
        sb.AppendLine(poste.DateCloture is DateTimeOffset dateCloture
            ? dateCloture.ToLocalTime().ToString("d MMMM yyyy", culture)
            : NotProvidedF());
        sb.AppendLine();

        if (criteres.Count > 0)
        {
            sb.AppendLine($"## {Pt("Profil recherché – critères et niveaux attendus", "Required profile – criteria and expected levels")}");
            foreach (var c in criteres.OrderBy(x => x.Categorie).ThenBy(x => x.Libelle))
            {
                var level = NiveauEvaluationLabels.Label(c.NiveauRequis, english);
                sb.AppendLine($"- {c.Libelle} ({c.Categorie}) : {level}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine(Pt(
            "Génère le texte complet de l'offre d'emploi, prêt à être mis en forme dans un document Word. Ne pas inclure de balises Markdown dans la sortie finale ; utilise des paragraphes et des listes en texte brut.",
            "Generate the full job offer text, ready to be formatted in a Word document. Do not include Markdown tags in the final output; use plain-text paragraphs and lists."));
        return sb.ToString();
    }

    private static byte[] BuildDocxInternal(
        string title,
        string bodyText,
        string colorPrimary,
        string colorSecondary,
        string colorBackground,
        bool english)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            var headerPara = new Paragraph();
            var shadingPPr = headerPara.AppendChild(new ParagraphProperties());
            shadingPPr.AppendChild(new Shading
            {
                Val = ShadingPatternValues.Clear,
                Color = "auto",
                Fill = colorPrimary
            });
            shadingPPr.AppendChild(new SpacingBetweenLines { Before = "320", After = "320" });
            shadingPPr.AppendChild(new Justification { Val = JustificationValues.Center });

            var hRun = new Run();
            var hRPr = new RunProperties();
            hRPr.AppendChild(new Bold());
            hRPr.AppendChild(new FontSize { Val = "48" });
            hRPr.AppendChild(new FontSizeComplexScript { Val = "48" });
            hRPr.AppendChild(new Color { Val = "FFFFFF" });
            hRPr.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
            hRun.AppendChild(hRPr);
            hRun.AppendChild(new Text(EscapeForXml(title.ToUpperInvariant())));
            headerPara.AppendChild(hRun);
            body.AppendChild(headerPara);

            var subtitlePara = new Paragraph();
            var subtitlePPr = new ParagraphProperties();
            subtitlePPr.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = colorBackground });
            subtitlePPr.AppendChild(new SpacingBetweenLines { Before = "160", After = "160" });
            subtitlePPr.AppendChild(new Justification { Val = JustificationValues.Center });
            subtitlePara.AppendChild(subtitlePPr);
            var stRun = new Run();
            var stRPr = new RunProperties();
            stRPr.AppendChild(new FontSize { Val = "24" });
            stRPr.AppendChild(new Color { Val = "1B3A6B" });
            stRPr.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
            stRun.AppendChild(stRPr);
            stRun.AppendChild(new Text(english ? "JOB OFFER" : "OFFRE D'EMPLOI"));
            subtitlePara.AppendChild(stRun);
            body.AppendChild(subtitlePara);

            body.AppendChild(MakeSpacerParagraph());

            var lines = bodyText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            var skipFirstHeading = true;

            foreach (var raw in lines)
            {
                var line = StripMarkdown(raw.Trim());

                if (string.IsNullOrEmpty(line) || line == "---")
                {
                    body.AppendChild(MakeSpacerParagraph());
                    continue;
                }

                var isHeading = line == line.ToUpperInvariant()
                    && line.Length > 3
                    && !line.StartsWith('•')
                    && !line.StartsWith('-')
                    && Regex.IsMatch(line, @"[A-ZÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖÙÚÛÜ]");

                if (isHeading)
                {
                    if (skipFirstHeading)
                    {
                        skipFirstHeading = false;
                        continue;
                    }

                    body.AppendChild(MakeSectionHeading(line, colorSecondary));
                    continue;
                }

                if (line.StartsWith("• ", StringComparison.Ordinal)
                    || line.StartsWith("- ", StringComparison.Ordinal)
                    || line.StartsWith("* ", StringComparison.Ordinal))
                {
                    body.AppendChild(MakeBulletParagraph(line[2..].Trim()));
                    continue;
                }

                body.AppendChild(MakeBodyParagraph(line));
            }

            body.AppendChild(MakeSpacerParagraph());
            var footerPara = new Paragraph();
            var fPPr = new ParagraphProperties();
            fPPr.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = colorPrimary });
            fPPr.AppendChild(new SpacingBetweenLines { Before = "120", After = "120" });
            fPPr.AppendChild(new Justification { Val = JustificationValues.Center });
            footerPara.AppendChild(fPPr);
            var fRun = new Run();
            var fRPr = new RunProperties();
            fRPr.AppendChild(new FontSize { Val = "18" });
            fRPr.AppendChild(new Color { Val = "FFFFFF" });
            fRPr.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
            fRun.AppendChild(fRPr);
            fRun.AppendChild(new Text(english
                ? "We look forward to meeting you."
                : "Nous avons hâte de vous rencontrer."));
            footerPara.AppendChild(fRun);
            body.AppendChild(footerPara);

            var sectPr = new SectionProperties();
            sectPr.AppendChild(new PageSize { Width = 11906, Height = 16838 });
            sectPr.AppendChild(new PageMargin { Top = 1440, Bottom = 1440, Left = 1080, Right = 1080 });
            body.AppendChild(sectPr);

            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static Paragraph MakeSectionHeading(string text, string colorSecondary)
    {
        var p = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = colorSecondary });
        pPr.AppendChild(new SpacingBetweenLines { Before = "280", After = "140" });
        pPr.AppendChild(new Indentation { Left = "240", Right = "240" });
        p.AppendChild(pPr);

        var run = new Run();
        var rPr = new RunProperties();
        rPr.AppendChild(new Bold());
        rPr.AppendChild(new FontSize { Val = "24" });
        rPr.AppendChild(new Color { Val = "FFFFFF" });
        rPr.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
        run.AppendChild(rPr);
        run.AppendChild(new Text(EscapeForXml(text)));
        p.AppendChild(run);
        return p;
    }

    private static Paragraph MakeBulletParagraph(string text)
    {
        var p = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.AppendChild(new SpacingBetweenLines { After = "80" });
        pPr.AppendChild(new Indentation { Left = "960", Hanging = "360" });
        p.AppendChild(pPr);

        var run = new Run();
        var rPr = new RunProperties();
        rPr.AppendChild(new FontSize { Val = "22" });
        rPr.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
        run.AppendChild(rPr);
        run.AppendChild(new Text(EscapeForXml("▪  " + text)) { Space = SpaceProcessingModeValues.Preserve });
        p.AppendChild(run);
        return p;
    }

    private static Paragraph MakeBodyParagraph(string text)
    {
        var p = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.AppendChild(new SpacingBetweenLines { After = "100", Line = "276", LineRule = LineSpacingRuleValues.Auto });
        pPr.AppendChild(new Justification { Val = JustificationValues.Both });
        pPr.AppendChild(new Indentation { Left = "240", Right = "240" });
        p.AppendChild(pPr);

        var run = new Run();
        var rPr = new RunProperties();
        rPr.AppendChild(new FontSize { Val = "22" });
        rPr.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
        run.AppendChild(rPr);
        run.AppendChild(new Text(EscapeForXml(text)) { Space = SpaceProcessingModeValues.Preserve });
        p.AppendChild(run);
        return p;
    }

    private static Paragraph MakeSpacerParagraph()
    {
        var p = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.AppendChild(new SpacingBetweenLines { Before = "0", After = "80" });
        p.AppendChild(pPr);
        return p;
    }

    private static string StripMarkdown(string text) =>
        Regex.Replace(text, @"\*\*(.+?)\*\*", "$1")
            .Replace("**", "", StringComparison.Ordinal)
            .Replace("__", "", StringComparison.Ordinal);

    private static string EscapeForXml(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        return s
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Poste";
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim().TrimEnd('.');
    }
}
