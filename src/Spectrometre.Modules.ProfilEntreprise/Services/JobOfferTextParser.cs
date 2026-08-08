using System.Text;
using System.Text.RegularExpressions;

namespace Spectrometre.Modules.ProfilEntreprise.Services;

/// <summary>Bloc sémantique d'un texte d'offre (même détection de titres/puces que <c>BuildDocxInternal</c>).</summary>
public enum JobOfferBlockKind
{
    Heading,
    Bullet,
    Paragraph,
    Spacer,
}

public sealed record JobOfferBlock(JobOfferBlockKind Kind, string Text);

/// <summary>
/// Parsing partagé texte d'offre → blocs (HTML candidat / OpenXML entreprise).
/// </summary>
public static class JobOfferTextParser
{
    public static bool IsSectionHeading(string line)
    {
        if (string.IsNullOrEmpty(line) || line.Length <= 3)
            return false;
        if (line.StartsWith('•') || line.StartsWith('-') || line.StartsWith('*'))
            return false;
        if (line != line.ToUpperInvariant())
            return false;
        return Regex.IsMatch(line, @"[A-ZÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖÙÚÛÜ]");
    }

    public static string StripMarkdown(string text) =>
        Regex.Replace(text, @"\*\*(.+?)\*\*", "$1")
            .Replace("**", "", StringComparison.Ordinal)
            .Replace("__", "", StringComparison.Ordinal);

    /// <summary>
    /// Découpe le corps d'offre en blocs. <paramref name="skipFirstHeading"/> ignore le premier
    /// titre MAJUSCULES (souvent le titre du poste, déjà affiché ailleurs).
    /// </summary>
    public static IReadOnlyList<JobOfferBlock> Parse(string? bodyText, bool skipFirstHeading = true)
    {
        if (string.IsNullOrWhiteSpace(bodyText))
            return [];

        var blocks = new List<JobOfferBlock>();
        var lines = bodyText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var skipHeading = skipFirstHeading;

        foreach (var raw in lines)
        {
            var line = StripMarkdown(raw.Trim());

            if (string.IsNullOrEmpty(line) || line == "---")
            {
                blocks.Add(new JobOfferBlock(JobOfferBlockKind.Spacer, ""));
                continue;
            }

            if (IsSectionHeading(line))
            {
                if (skipHeading)
                {
                    skipHeading = false;
                    continue;
                }

                blocks.Add(new JobOfferBlock(JobOfferBlockKind.Heading, line));
                continue;
            }

            if (line.StartsWith("• ", StringComparison.Ordinal)
                || line.StartsWith("- ", StringComparison.Ordinal)
                || line.StartsWith("* ", StringComparison.Ordinal))
            {
                blocks.Add(new JobOfferBlock(JobOfferBlockKind.Bullet, line[2..].Trim()));
                continue;
            }

            blocks.Add(new JobOfferBlock(JobOfferBlockKind.Paragraph, line));
        }

        return blocks;
    }
}
