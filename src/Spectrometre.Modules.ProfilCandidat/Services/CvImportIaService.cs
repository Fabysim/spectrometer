using System.Globalization;
using System.Text;
using System.Text.Json;
using Spectrometre.Core.Ai;
using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Adaptateur CV → <see cref="IReplicateService"/>. Les tests substituent
/// <see cref="ICvImportIaService"/> ; le noyau conserve <see cref="IReplicateService"/>.
/// </summary>
public sealed class CvImportIaService(IReplicateService replicate) : ICvImportIaService
{
    private const int MaxCaracteresPrompt = 40_000;

    public async Task<CvView?> ExtraireCvAsync(string texteDocument, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(texteDocument))
                return null;

            var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
            var texte = texteDocument.Trim();
            if (texte.Length > MaxCaracteresPrompt)
                texte = texte[..MaxCaracteresPrompt];

            var (output, error) = await replicate.RunClaudeAsync(
                BuildSystemPrompt(english),
                BuildUserPrompt(texte, english),
                cancellationToken);

            if (error is not null || string.IsNullOrWhiteSpace(output))
                return null;

            return ParseCv(output);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildSystemPrompt(bool english) => english
        ? """
You extract structured resume data from plain text.
Reply ONLY with valid JSON, no markdown, no text before or after, exact shape:
{
  "coordonnees": {
    "nom": "",
    "prenoms": "",
    "dateNaissance": "YYYY-MM-DD or empty",
    "lieuNaissance": "",
    "nationalite": "",
    "adresseComplete": "",
    "telephone": "",
    "email": "",
    "profilOuPosteRecherche": ""
  },
  "formations": [
    { "periode": "", "etablissement": "", "diplomeCertificatOuNiveau": "", "domaineEtudes": "" }
  ],
  "competencesEtudes": {
    "specialitePrincipale": "",
    "competencesTechniques": "",
    "connaissancesTheoriques": "",
    "languesMaitrisees": "",
    "outilsLogicielsMethodes": ""
  },
  "experiences": [
    { "periode": "", "entrepriseOrganisationOuStage": "", "fonctionOuActiviteExercee": "", "competencesDeveloppees": "" }
  ],
  "caracteristiquesPersonnelles": {
    "qualitesPersonnelles": "",
    "aptitudesProfessionnelles": "",
    "attitudesRelationnelles": "",
    "capaciteSousPression": "",
    "disponibiliteMobilite": ""
  },
  "loisirs": {
    "loisirsPreferes": "",
    "activitesSportivesCulturelles": "",
    "engagementsAssociatifs": "",
    "autresCentresInteret": ""
  },
  "references": [
    { "nomPrenom": "", "fonction": "", "entrepriseOrganisation": "", "telephoneOuEmail": "", "lienAvecPostulant": "" }
  ]
}
Use empty string or omit a field when unknown. Do not invent facts. Do not include a declaration/signature section.
"""
        : """
Tu extraits un curriculum vitæ structuré à partir d'un texte brut.
Réponds UNIQUEMENT en JSON valide, sans markdown, sans texte avant ou après, avec cette forme exacte :
{
  "coordonnees": {
    "nom": "",
    "prenoms": "",
    "dateNaissance": "AAAA-MM-JJ ou vide",
    "lieuNaissance": "",
    "nationalite": "",
    "adresseComplete": "",
    "telephone": "",
    "email": "",
    "profilOuPosteRecherche": ""
  },
  "formations": [
    { "periode": "", "etablissement": "", "diplomeCertificatOuNiveau": "", "domaineEtudes": "" }
  ],
  "competencesEtudes": {
    "specialitePrincipale": "",
    "competencesTechniques": "",
    "connaissancesTheoriques": "",
    "languesMaitrisees": "",
    "outilsLogicielsMethodes": ""
  },
  "experiences": [
    { "periode": "", "entrepriseOrganisationOuStage": "", "fonctionOuActiviteExercee": "", "competencesDeveloppees": "" }
  ],
  "caracteristiquesPersonnelles": {
    "qualitesPersonnelles": "",
    "aptitudesProfessionnelles": "",
    "attitudesRelationnelles": "",
    "capaciteSousPression": "",
    "disponibiliteMobilite": ""
  },
  "loisirs": {
    "loisirsPreferes": "",
    "activitesSportivesCulturelles": "",
    "engagementsAssociatifs": "",
    "autresCentresInteret": ""
  },
  "references": [
    { "nomPrenom": "", "fonction": "", "entrepriseOrganisation": "", "telephoneOuEmail": "", "lienAvecPostulant": "" }
  ]
}
Champ inconnu : chaîne vide ou omission. N'invente rien. N'inclus pas de déclaration / signature.
""";

    private static string BuildUserPrompt(string texte, bool english)
    {
        var sb = new StringBuilder();
        if (english)
        {
            sb.AppendLine("## Resume text");
            sb.AppendLine();
            sb.AppendLine(texte);
            sb.AppendLine();
            sb.AppendLine("## Request");
            sb.AppendLine("Fill the JSON from this text only.");
        }
        else
        {
            sb.AppendLine("## Texte du CV");
            sb.AppendLine();
            sb.AppendLine(texte);
            sb.AppendLine();
            sb.AppendLine("## Demande");
            sb.AppendLine("Remplis le JSON uniquement à partir de ce texte.");
        }

        return sb.ToString();
    }

    public static CvView? ParseCv(string output)
    {
        var json = ExtractJsonObject(output);
        if (json is null)
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var coordonnees = LireCoordonnees(root);
            var formations = LireFormations(root);
            var competences = LireCompetences(root);
            var experiences = LireExperiences(root);
            var caracteristiques = LireCaracteristiques(root);
            var loisirs = LireLoisirs(root);
            var references = LireReferences(root);

            var view = new CvView(
                coordonnees,
                formations,
                competences,
                experiences,
                caracteristiques,
                loisirs,
                references,
                Declaration: null);

            return EstVide(view) ? null : view;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static bool EstVide(CvView view) =>
        view.Coordonnees is null
        && view.Formations.Count == 0
        && view.CompetencesEtudes is null
        && view.Experiences.Count == 0
        && view.CaracteristiquesPersonnelles is null
        && view.Loisirs is null
        && view.References.Count == 0;

    private static CvCoordonnees? LireCoordonnees(JsonElement root)
    {
        if (!TryGetObject(root, "coordonnees", out var o))
            return null;

        var entity = new CvCoordonnees
        {
            Nom = ReadString(o, "nom"),
            Prenoms = ReadString(o, "prenoms"),
            DateNaissance = ReadDate(o, "dateNaissance"),
            LieuNaissance = ReadString(o, "lieuNaissance"),
            Nationalite = ReadString(o, "nationalite"),
            AdresseComplete = ReadString(o, "adresseComplete"),
            Telephone = ReadString(o, "telephone"),
            Email = ReadString(o, "email"),
            ProfilOuPosteRecherche = ReadString(o, "profilOuPosteRecherche"),
        };

        return TousVides(
            entity.Nom, entity.Prenoms, entity.LieuNaissance, entity.Nationalite,
            entity.AdresseComplete, entity.Telephone, entity.Email, entity.ProfilOuPosteRecherche)
            && entity.DateNaissance is null
            ? null
            : entity;
    }

    private static IReadOnlyList<CvFormation> LireFormations(JsonElement root)
    {
        if (!TryGetArray(root, "formations", out var array))
            return [];

        var list = new List<CvFormation>();
        foreach (var item in array.EnumerateArray())
        {
            var entity = new CvFormation
            {
                Periode = ReadString(item, "periode"),
                Etablissement = ReadString(item, "etablissement"),
                DiplomeCertificatOuNiveau = ReadString(item, "diplomeCertificatOuNiveau"),
                DomaineEtudes = ReadString(item, "domaineEtudes"),
            };
            if (TousVides(entity.Periode, entity.Etablissement, entity.DiplomeCertificatOuNiveau, entity.DomaineEtudes))
                continue;
            list.Add(entity);
        }

        return list;
    }

    private static CvCompetencesEtudes? LireCompetences(JsonElement root)
    {
        if (!TryGetObject(root, "competencesEtudes", out var o))
            return null;

        var entity = new CvCompetencesEtudes
        {
            SpecialitePrincipale = ReadString(o, "specialitePrincipale"),
            CompetencesTechniques = ReadString(o, "competencesTechniques"),
            ConnaissancesTheoriques = ReadString(o, "connaissancesTheoriques"),
            LanguesMaitrisees = ReadString(o, "languesMaitrisees"),
            OutilsLogicielsMethodes = ReadString(o, "outilsLogicielsMethodes"),
        };

        return TousVides(
            entity.SpecialitePrincipale, entity.CompetencesTechniques, entity.ConnaissancesTheoriques,
            entity.LanguesMaitrisees, entity.OutilsLogicielsMethodes)
            ? null
            : entity;
    }

    private static IReadOnlyList<CvExperience> LireExperiences(JsonElement root)
    {
        if (!TryGetArray(root, "experiences", out var array))
            return [];

        var list = new List<CvExperience>();
        foreach (var item in array.EnumerateArray())
        {
            var entity = new CvExperience
            {
                Periode = ReadString(item, "periode"),
                EntrepriseOrganisationOuStage = ReadString(item, "entrepriseOrganisationOuStage"),
                FonctionOuActiviteExercee = ReadString(item, "fonctionOuActiviteExercee"),
                CompetencesDeveloppees = ReadString(item, "competencesDeveloppees"),
            };
            if (TousVides(
                    entity.Periode, entity.EntrepriseOrganisationOuStage,
                    entity.FonctionOuActiviteExercee, entity.CompetencesDeveloppees))
                continue;
            list.Add(entity);
        }

        return list;
    }

    private static CvCaracteristiquesPersonnelles? LireCaracteristiques(JsonElement root)
    {
        if (!TryGetObject(root, "caracteristiquesPersonnelles", out var o))
            return null;

        var entity = new CvCaracteristiquesPersonnelles
        {
            QualitesPersonnelles = ReadString(o, "qualitesPersonnelles"),
            AptitudesProfessionnelles = ReadString(o, "aptitudesProfessionnelles"),
            AttitudesRelationnelles = ReadString(o, "attitudesRelationnelles"),
            CapaciteSousPression = ReadString(o, "capaciteSousPression"),
            DisponibiliteMobilite = ReadString(o, "disponibiliteMobilite"),
        };

        return TousVides(
            entity.QualitesPersonnelles, entity.AptitudesProfessionnelles, entity.AttitudesRelationnelles,
            entity.CapaciteSousPression, entity.DisponibiliteMobilite)
            ? null
            : entity;
    }

    private static CvLoisirs? LireLoisirs(JsonElement root)
    {
        if (!TryGetObject(root, "loisirs", out var o))
            return null;

        var entity = new CvLoisirs
        {
            LoisirsPreferes = ReadString(o, "loisirsPreferes"),
            ActivitesSportivesCulturelles = ReadString(o, "activitesSportivesCulturelles"),
            EngagementsAssociatifs = ReadString(o, "engagementsAssociatifs"),
            AutresCentresInteret = ReadString(o, "autresCentresInteret"),
        };

        return TousVides(
            entity.LoisirsPreferes, entity.ActivitesSportivesCulturelles,
            entity.EngagementsAssociatifs, entity.AutresCentresInteret)
            ? null
            : entity;
    }

    private static IReadOnlyList<CvReference> LireReferences(JsonElement root)
    {
        if (!TryGetArray(root, "references", out var array))
            return [];

        var list = new List<CvReference>();
        foreach (var item in array.EnumerateArray())
        {
            var entity = new CvReference
            {
                NomPrenom = ReadString(item, "nomPrenom"),
                Fonction = ReadString(item, "fonction"),
                EntrepriseOrganisation = ReadString(item, "entrepriseOrganisation"),
                TelephoneOuEmail = ReadString(item, "telephoneOuEmail"),
                LienAvecPostulant = ReadString(item, "lienAvecPostulant"),
            };
            if (TousVides(
                    entity.NomPrenom, entity.Fonction, entity.EntrepriseOrganisation,
                    entity.TelephoneOuEmail, entity.LienAvecPostulant))
                continue;
            list.Add(entity);
        }

        return list;
    }

    private static bool TryGetObject(JsonElement root, string name, out JsonElement obj)
    {
        obj = default;
        return root.TryGetProperty(name, out obj) && obj.ValueKind == JsonValueKind.Object;
    }

    private static bool TryGetArray(JsonElement root, string name, out JsonElement array)
    {
        array = default;
        return root.TryGetProperty(name, out array) && array.ValueKind == JsonValueKind.Array;
    }

    private static string? ReadString(JsonElement item, string name)
    {
        if (!item.TryGetProperty(name, out var prop))
            return null;
        if (prop.ValueKind != JsonValueKind.String)
            return null;
        var value = prop.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateOnly? ReadDate(JsonElement item, string name)
    {
        var raw = ReadString(item, name);
        if (raw is null)
            return null;
        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (DateOnly.TryParse(raw, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out d))
            return d;
        return DateOnly.TryParse(raw, out d) ? d : null;
    }

    private static bool TousVides(params string?[] values) =>
        values.All(string.IsNullOrWhiteSpace);

    private static string? ExtractJsonObject(string output)
    {
        var trimmed = output.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = trimmed.IndexOf('\n');
            if (firstNl >= 0)
                trimmed = trimmed[(firstNl + 1)..];
            var fence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fence >= 0)
                trimmed = trimmed[..fence];
            trimmed = trimmed.Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        return trimmed[start..(end + 1)];
    }
}
