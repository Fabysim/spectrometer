namespace Spectrometre.Modules.ProfilEntreprise.Services;

/// <summary>
/// Suggestion IA de critères de poste (texte libre → catégorie / libellé / niveau 0–4).
/// Même robustesse que <see cref="IAnalysePosteIaService"/> : JAMAIS d'exception — liste vide en repli.
/// </summary>
public interface IPosteCritereIaService
{
    /// <summary>
    /// Propose des critères à partir des champs texte du poste (pas de catalogue ProfileItem).
    /// Liste vide si non configuré / timeout / JSON invalide / réponse vide.
    /// </summary>
    Task<IReadOnlyList<(string Categorie, string Libelle, int NiveauRequis)>> SuggererCriteresAsync(
        string titrePoste,
        string? description,
        string? tachesDescription,
        string? competencesRequises,
        CancellationToken cancellationToken = default);
}
