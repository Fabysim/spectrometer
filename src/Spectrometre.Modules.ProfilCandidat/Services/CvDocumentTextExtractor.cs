using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace Spectrometre.Modules.ProfilCandidat.Services;

public sealed class CvDocumentTextExtractor : ICvDocumentTextExtractor
{
    public bool EstFormatAccepte(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName);
        if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return true;
        if (ext.Equals(".docx", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(
                contentType,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public async Task<string?> ExtraireAsync(
        Stream contenu,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var buffer = new MemoryStream();
            await contenu.CopyToAsync(buffer, cancellationToken);
            if (buffer.Length == 0)
                return null;

            buffer.Position = 0;
            var ext = Path.GetExtension(fileName);

            if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase) || EstPdf(buffer))
                return ExtrairePdf(buffer);

            if (ext.Equals(".docx", StringComparison.OrdinalIgnoreCase) || EstZip(buffer))
                return ExtraireDocx(buffer);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool EstPdf(MemoryStream buffer)
    {
        if (buffer.Length < 4)
            return false;
        var span = buffer.GetBuffer().AsSpan(0, 4);
        return span[0] == (byte)'%' && span[1] == (byte)'P' && span[2] == (byte)'D' && span[3] == (byte)'F';
    }

    private static bool EstZip(MemoryStream buffer)
    {
        if (buffer.Length < 2)
            return false;
        var span = buffer.GetBuffer().AsSpan(0, 2);
        return span[0] == (byte)'P' && span[1] == (byte)'K';
    }

    private static string? ExtrairePdf(MemoryStream buffer)
    {
        buffer.Position = 0;
        using var document = PdfDocument.Open(buffer);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            if (!string.IsNullOrWhiteSpace(page.Text))
                sb.AppendLine(page.Text);
        }

        var texte = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(texte) ? null : texte;
    }

    private static string? ExtraireDocx(MemoryStream buffer)
    {
        buffer.Position = 0;
        using var document = WordprocessingDocument.Open(buffer, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
            return null;

        var sb = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var line = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(line))
                sb.AppendLine(line);
        }

        var texte = sb.ToString().Trim();
        return string.IsNullOrWhiteSpace(texte) ? null : texte;
    }
}
