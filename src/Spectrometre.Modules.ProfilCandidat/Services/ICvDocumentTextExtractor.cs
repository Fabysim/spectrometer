namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Extraction locale de texte d'un CV PDF ou Word (.docx) — PdfPig (Apache-2.0) et
/// DocumentFormat.OpenXml (MIT). Pas d'appel réseau ; seul le mapping ultérieur passe par
/// <see cref="Spectrometre.Core.Ai.IReplicateService"/>.
/// </summary>
public interface ICvDocumentTextExtractor
{
    const int TailleMaxOctets = 5 * 1024 * 1024;

    bool EstFormatAccepte(string fileName, string? contentType);

    /// <summary>Texte extrait, ou <c>null</c> si le fichier est illisible / vide.</summary>
    Task<string?> ExtraireAsync(Stream contenu, string fileName, CancellationToken cancellationToken = default);
}
