using System.Globalization;
using System.Text;
using System.Text.Json;
using Spectrometre.Core.Ai;

namespace Spectrometre.Modules.ProfilEntreprise.Services;

/// <summary>
/// Adaptateur PostesRecrutement → <see cref="IReplicateService"/> pour la suggestion de critères.
/// Même câblage que <see cref="ReplicateAnalysePosteIaService"/> : les tests substituent
/// <see cref="IPosteCritereIaService"/> ; le noyau conserve <see cref="IReplicateService"/>.
/// </summary>
public sealed class PosteCritereIaService(IReplicateService replicate) : IPosteCritereIaService
{
    public async Task<IReadOnlyList<(string Categorie, string Libelle, int NiveauRequis)>> SuggererCriteresAsync(
        string titrePoste,
        string? description,
        string? tachesDescription,
        string? competencesRequises,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
            var systemPrompt = BuildSystemPrompt(english);
            var userPrompt = BuildUserPrompt(titrePoste, description, tachesDescription, competencesRequises, english);
            var (output, error) = await replicate.RunClaudeAsync(systemPrompt, userPrompt, cancellationToken);

            if (error is not null || string.IsNullOrWhiteSpace(output))
                return [];

            return ParseSuggestions(output);
        }
        catch
        {
            // Filet de sécurité : jamais d'exception jusqu'à l'appelant (même pattern que l'analyse IA).
            return [];
        }
    }

    private static string BuildSystemPrompt(bool english) => english
        ? """
You are an expert in competency assessment, recruitment and human resources.
Analyse a job posting and propose relevant evaluation criteria (category + label + required level).
Do NOT use a fixed catalogue — invent concise, job-specific criteria.
Required level scale 0–4:
- 0 = Not required at all
- 1 = Weak
- 2 = Medium
- 3 = Strong
- 4 = Very strong (fundamental for the role)

Reply ONLY with valid JSON, no text before or after, exact format:
{
  "criteres": [
    {
      "categorie": "<short category>",
      "libelle": "<criterion label>",
      "niveauRequis": <integer 0-4>
    }
  ]
}
Use English for categorie and libelle.
"""
        : """
Tu es un expert en évaluation des compétences, en recrutement et en ressources humaines.
Analyse un poste à pourvoir et propose des critères d'évaluation pertinents (catégorie + libellé + niveau requis).
N'utilise PAS un catalogue fixe — invente des critères concis et spécifiques au poste.
Échelle de niveau requis 0–4 :
- 0 = Pas du tout requis
- 1 = Faible
- 2 = Moyen
- 3 = Fort
- 4 = Très fort (fondamental pour le poste)

Réponds UNIQUEMENT en JSON valide, sans texte avant ou après, avec ce format exact :
{
  "criteres": [
    {
      "categorie": "<catégorie courte>",
      "libelle": "<libellé du critère>",
      "niveauRequis": <entier 0-4>
    }
  ]
}
Utilise le français pour categorie et libelle.
""";

    private static string BuildUserPrompt(
        string titrePoste,
        string? description,
        string? tachesDescription,
        string? competencesRequises,
        bool english)
    {
        var sb = new StringBuilder();
        if (english)
        {
            sb.AppendLine("## Job description");
            sb.AppendLine();
            sb.AppendLine($"**Job title:** {titrePoste}");
            sb.AppendLine($"**Description:** {NullOrText(description, english)}");
            sb.AppendLine($"**Required skills:** {NullOrText(competencesRequises, english)}");
            sb.AppendLine($"**Main tasks:** {NullOrText(tachesDescription, english)}");
            sb.AppendLine();
            sb.AppendLine("## Request");
            sb.AppendLine();
            sb.AppendLine("Propose all relevant criteria to evaluate a candidate for THIS specific role (typically 5–15).");
            sb.AppendLine("Assign a required level (0–4) for each. Prefer actionable, observable criteria.");
        }
        else
        {
            sb.AppendLine("## Description du poste");
            sb.AppendLine();
            sb.AppendLine($"**Intitulé du poste :** {titrePoste}");
            sb.AppendLine($"**Description :** {NullOrText(description, english)}");
            sb.AppendLine($"**Compétences requises :** {NullOrText(competencesRequises, english)}");
            sb.AppendLine($"**Tâches principales :** {NullOrText(tachesDescription, english)}");
            sb.AppendLine();
            sb.AppendLine("## Demande");
            sb.AppendLine();
            sb.AppendLine("Propose tous les critères pertinents pour évaluer un candidat à CE poste (typiquement 5–15).");
            sb.AppendLine("Attribue un niveau requis (0–4) pour chacun. Privilégie des critères actionnables et observables.");
        }

        return sb.ToString();
    }

    private static string NullOrText(string? value, bool english) =>
        string.IsNullOrWhiteSpace(value)
            ? (english ? "(not provided)" : "(non fourni)")
            : value.Trim();

    private static IReadOnlyList<(string Categorie, string Libelle, int NiveauRequis)> ParseSuggestions(string output)
    {
        var json = ExtractJsonObject(output);
        if (json is null)
            return [];

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("criteres", out var array)
                && !doc.RootElement.TryGetProperty("suggestions", out array))
                return [];

            if (array.ValueKind != JsonValueKind.Array)
                return [];

            var results = new List<(string, string, int)>();
            foreach (var item in array.EnumerateArray())
            {
                var categorie = ReadString(item, "categorie") ?? ReadString(item, "category");
                var libelle = ReadString(item, "libelle") ?? ReadString(item, "label") ?? ReadString(item, "name");
                if (string.IsNullOrWhiteSpace(categorie) || string.IsNullOrWhiteSpace(libelle))
                    continue;

                var niveau = 2;
                if (item.TryGetProperty("niveauRequis", out var n)
                    || item.TryGetProperty("suggestedLevel", out n)
                    || item.TryGetProperty("niveau", out n))
                {
                    if (n.ValueKind == JsonValueKind.Number && n.TryGetInt32(out var parsed))
                        niveau = parsed;
                    else if (n.ValueKind == JsonValueKind.String && int.TryParse(n.GetString(), out parsed))
                        niveau = parsed;
                }

                results.Add((categorie.Trim(), libelle.Trim(), Math.Clamp(niveau, 0, 4)));
            }

            return results;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ReadString(JsonElement item, string name) =>
        item.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    /// <summary>Extrait le premier objet JSON d'une réponse éventuellement entourée de markdown.</summary>
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
