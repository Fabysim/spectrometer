using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Mapping IA texte de CV → <see cref="CvView"/>. JAMAIS d'exception — <c>null</c> en repli
/// (même défense que <c>IPosteCritereIaService</c>). N'enregistre rien : le candidat relit et confirme.
/// </summary>
public interface ICvImportIaService
{
    Task<CvView?> ExtraireCvAsync(string texteDocument, CancellationToken cancellationToken = default);
}

public sealed record CvImportResultat(bool Success, string MessageKey, CvView? Brouillon);

/// <summary>
/// Orchestration import : validation fichier → extraction locale → mapping IA.
/// Aucune écriture en base.
/// </summary>
public interface ICvImportService
{
    Task<CvImportResultat> ImporterAsync(
        Stream contenu,
        string fileName,
        string? contentType,
        long tailleOctets,
        CancellationToken cancellationToken = default);
}
