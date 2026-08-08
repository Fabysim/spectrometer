using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>Implémentation réelle de <see cref="ICvPdfService"/> — voir sa remarque.</summary>
public sealed class CvPdfService : ICvPdfService
{
    static CvPdfService()
    {
        // Licence Community — même choix que mvp (PdfReportGenerator), gratuite pour ce contexte.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateCvPdf(CvView cv)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("Curriculum Vitæ").FontSize(18).Bold();

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(12);

                    if (cv.Coordonnees is { } coord)
                    {
                        column.Item().Element(c => Section(c, "1. Coordonnées du postulant", section =>
                        {
                            Field(section, "Nom", coord.Nom);
                            Field(section, "Prénoms", coord.Prenoms);
                            Field(section, "Date de naissance", coord.DateNaissance?.ToString("yyyy-MM-dd"));
                            Field(section, "Lieu de naissance", coord.LieuNaissance);
                            Field(section, "Nationalité", coord.Nationalite);
                            Field(section, "Adresse", coord.AdresseComplete);
                            Field(section, "Téléphone", coord.Telephone);
                            Field(section, "Email", coord.Email);
                            Field(section, "Profil ou poste recherché", coord.ProfilOuPosteRecherche);
                        }));
                    }

                    if (cv.Formations.Count > 0)
                    {
                        column.Item().Element(c => Section(c, "2. Études réussies et diplômes obtenus", section =>
                        {
                            foreach (var f in cv.Formations.OrderBy(x => x.DisplayOrder))
                            {
                                section.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span($"{f.Periode} — {f.Etablissement}").Bold();
                                });
                                Field(section, "Diplôme/certificat/niveau", f.DiplomeCertificatOuNiveau);
                                Field(section, "Domaine d'études", f.DomaineEtudes);
                            }
                        }));
                    }

                    if (cv.CompetencesEtudes is { } comp)
                    {
                        column.Item().Element(c => Section(c, "3. Spécialités et compétences acquises par les études", section =>
                        {
                            Field(section, "Spécialité principale", comp.SpecialitePrincipale);
                            Field(section, "Compétences techniques", comp.CompetencesTechniques);
                            Field(section, "Connaissances théoriques", comp.ConnaissancesTheoriques);
                            Field(section, "Langues maîtrisées", comp.LanguesMaitrisees);
                            Field(section, "Outils, logiciels, méthodes", comp.OutilsLogicielsMethodes);
                        }));
                    }

                    if (cv.Experiences.Count > 0)
                    {
                        column.Item().Element(c => Section(c, "4. Compétences acquises par les expériences pratiques", section =>
                        {
                            foreach (var e in cv.Experiences.OrderBy(x => x.DisplayOrder))
                            {
                                section.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span($"{e.Periode} — {e.EntrepriseOrganisationOuStage}").Bold();
                                });
                                Field(section, "Fonction ou activité exercée", e.FonctionOuActiviteExercee);
                                Field(section, "Compétences développées", e.CompetencesDeveloppees);
                            }
                        }));
                    }

                    if (cv.CaracteristiquesPersonnelles is { } carac)
                    {
                        column.Item().Element(c => Section(c, "5. Caractéristiques personnelles", section =>
                        {
                            Field(section, "Qualités personnelles", carac.QualitesPersonnelles);
                            Field(section, "Aptitudes professionnelles", carac.AptitudesProfessionnelles);
                            Field(section, "Attitudes relationnelles", carac.AttitudesRelationnelles);
                            Field(section, "Capacité sous pression", carac.CapaciteSousPression);
                            Field(section, "Disponibilité / mobilité", carac.DisponibiliteMobilite);
                        }));
                    }

                    if (cv.Loisirs is { } loisirs)
                    {
                        column.Item().Element(c => Section(c, "6. Loisirs et centres d'intérêt", section =>
                        {
                            Field(section, "Loisirs préférés", loisirs.LoisirsPreferes);
                            Field(section, "Activités sportives/culturelles", loisirs.ActivitesSportivesCulturelles);
                            Field(section, "Engagements associatifs", loisirs.EngagementsAssociatifs);
                            Field(section, "Autres centres d'intérêt", loisirs.AutresCentresInteret);
                        }));
                    }

                    if (cv.References.Count > 0)
                    {
                        column.Item().Element(c => Section(c, "7. Références professionnelles", section =>
                        {
                            foreach (var r in cv.References.OrderBy(x => x.DisplayOrder))
                            {
                                section.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span($"{r.NomPrenom} — {r.Fonction}").Bold();
                                });
                                Field(section, "Entreprise/organisation", r.EntrepriseOrganisation);
                                Field(section, "Téléphone ou email", r.TelephoneOuEmail);
                                Field(section, "Lien avec le postulant", r.LienAvecPostulant);
                            }
                        }));
                    }

                    if (cv.Declaration is { } decl)
                    {
                        column.Item().Element(c => Section(c, "8. Déclaration du postulant", section =>
                        {
                            Field(section, "Certification d'exactitude", decl.CertificationExactitude ? "Oui" : "Non");
                            Field(section, "Consentement à la consultation", decl.ConsentementConsultation ? "Oui" : "Non");
                            Field(section, "Date", decl.Date?.ToString("yyyy-MM-dd"));
                            Field(section, "Signataire", decl.NomSignataire);
                        }));
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void Section(QuestPDF.Infrastructure.IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Column(column =>
        {
            column.Item().Text(title).FontSize(13).Bold();
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
}
