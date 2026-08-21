using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>Implémentation réelle de <see cref="ICvWordService"/> — voir sa remarque.</summary>
public sealed class CvWordService : ICvWordService
{
    public byte[] GenerateCvWord(CvView cv)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            body.AppendChild(Paragraph("Curriculum Vitæ", bold: true, fontSizeHalfPoints: "36"));

            if (cv.Coordonnees is { } coord)
            {
                Section(body, "1. Coordonnées du postulant");
                Field(body, "Nom", coord.Nom);
                Field(body, "Prénoms", coord.Prenoms);
                Field(body, "Date de naissance", coord.DateNaissance?.ToString("yyyy-MM-dd"));
                Field(body, "Lieu de naissance", coord.LieuNaissance);
                Field(body, "Nationalité", coord.Nationalite);
                Field(body, "Adresse", coord.AdresseComplete);
                Field(body, "Téléphone", coord.Telephone);
                Field(body, "Email", coord.Email);
                Field(body, "Profil ou poste recherché", coord.ProfilOuPosteRecherche);
            }

            if (cv.Formations.Count > 0)
            {
                Section(body, "2. Études réussies et diplômes obtenus");
                foreach (var f in cv.Formations.OrderBy(x => x.DisplayOrder))
                {
                    body.AppendChild(Paragraph($"{f.Periode} — {f.Etablissement}", bold: true));
                    Field(body, "Diplôme/certificat/niveau", f.DiplomeCertificatOuNiveau);
                    Field(body, "Domaine d'études", f.DomaineEtudes);
                }
            }

            if (cv.CompetencesEtudes is { } comp)
            {
                Section(body, "3. Spécialités et compétences acquises par les études");
                Field(body, "Spécialité principale", comp.SpecialitePrincipale);
                Field(body, "Compétences techniques", comp.CompetencesTechniques);
                Field(body, "Connaissances théoriques", comp.ConnaissancesTheoriques);
                Field(body, "Langues maîtrisées", comp.LanguesMaitrisees);
                Field(body, "Outils, logiciels, méthodes", comp.OutilsLogicielsMethodes);
            }

            if (cv.Experiences.Count > 0)
            {
                Section(body, "4. Compétences acquises par les expériences pratiques");
                foreach (var e in cv.Experiences.OrderBy(x => x.DisplayOrder))
                {
                    body.AppendChild(Paragraph($"{e.Periode} — {e.EntrepriseOrganisationOuStage}", bold: true));
                    Field(body, "Fonction ou activité exercée", e.FonctionOuActiviteExercee);
                    Field(body, "Compétences développées", e.CompetencesDeveloppees);
                }
            }

            if (cv.CaracteristiquesPersonnelles is { } carac)
            {
                Section(body, "5. Caractéristiques personnelles");
                Field(body, "Qualités personnelles", carac.QualitesPersonnelles);
                Field(body, "Aptitudes professionnelles", carac.AptitudesProfessionnelles);
                Field(body, "Attitudes relationnelles", carac.AttitudesRelationnelles);
                Field(body, "Capacité sous pression", carac.CapaciteSousPression);
                Field(body, "Disponibilité / mobilité", carac.DisponibiliteMobilite);
            }

            if (cv.Loisirs is { } loisirs)
            {
                Section(body, "6. Loisirs et centres d'intérêt");
                Field(body, "Loisirs préférés", loisirs.LoisirsPreferes);
                Field(body, "Activités sportives/culturelles", loisirs.ActivitesSportivesCulturelles);
                Field(body, "Engagements associatifs", loisirs.EngagementsAssociatifs);
                Field(body, "Autres centres d'intérêt", loisirs.AutresCentresInteret);
            }

            if (cv.References.Count > 0)
            {
                Section(body, "7. Références professionnelles");
                foreach (var r in cv.References.OrderBy(x => x.DisplayOrder))
                {
                    body.AppendChild(Paragraph($"{r.NomPrenom} — {r.Fonction}", bold: true));
                    Field(body, "Entreprise/organisation", r.EntrepriseOrganisation);
                    Field(body, "Téléphone ou email", r.TelephoneOuEmail);
                    Field(body, "Lien avec le postulant", r.LienAvecPostulant);
                }
            }

            if (cv.Declaration is { } decl)
            {
                Section(body, "8. Déclaration du postulant");
                Field(body, "Certification d'exactitude", decl.CertificationExactitude ? "Oui" : "Non");
                Field(body, "Consentement à la consultation", decl.ConsentementConsultation ? "Oui" : "Non");
                Field(body, "Date", decl.Date?.ToString("yyyy-MM-dd"));
                Field(body, "Signataire", decl.NomSignataire);
            }

            var sectPr = new SectionProperties();
            sectPr.AppendChild(new PageSize { Width = 11906, Height = 16838 });
            sectPr.AppendChild(new PageMargin { Top = 1440, Bottom = 1440, Left = 1440, Right = 1440 });
            body.AppendChild(sectPr);

            mainPart.Document.Save();
        }

        return ms.ToArray();
    }

    private static void Section(Body body, string title) =>
        body.AppendChild(Paragraph(title, bold: true, fontSizeHalfPoints: "26", spaceBefore: "240"));

    private static void Field(Body body, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        body.AppendChild(LabeledParagraph(label, value));
    }

    private static Paragraph Paragraph(string text, bool bold = false, string fontSizeHalfPoints = "20", string? spaceBefore = null)
    {
        var p = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.AppendChild(new SpacingBetweenLines
        {
            Before = spaceBefore ?? "80",
            After = "80",
        });
        p.AppendChild(pPr);

        var run = new Run();
        var rPr = new RunProperties();
        if (bold)
            rPr.AppendChild(new Bold());
        rPr.AppendChild(new FontSize { Val = fontSizeHalfPoints });
        rPr.AppendChild(new FontSizeComplexScript { Val = fontSizeHalfPoints });
        rPr.AppendChild(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
        run.AppendChild(rPr);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        p.AppendChild(run);
        return p;
    }

    private static Paragraph LabeledParagraph(string label, string value)
    {
        var p = new Paragraph();
        var pPr = new ParagraphProperties();
        pPr.AppendChild(new SpacingBetweenLines { After = "60" });
        p.AppendChild(pPr);

        var labelRun = new Run();
        var labelRPr = new RunProperties();
        labelRPr.AppendChild(new Bold());
        labelRPr.AppendChild(new FontSize { Val = "20" });
        labelRPr.AppendChild(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
        labelRun.AppendChild(labelRPr);
        labelRun.AppendChild(new Text($"{label} : ") { Space = SpaceProcessingModeValues.Preserve });
        p.AppendChild(labelRun);

        var valueRun = new Run();
        var valueRPr = new RunProperties();
        valueRPr.AppendChild(new FontSize { Val = "20" });
        valueRPr.AppendChild(new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" });
        valueRun.AppendChild(valueRPr);
        valueRun.AppendChild(new Text(value.ReplaceLineEndings(" ")) { Space = SpaceProcessingModeValues.Preserve });
        p.AppendChild(valueRun);
        return p;
    }
}
