using Spectrometre.Core.Recruitment;
using Spectrometre.Modules.Recrutement.Entities;

namespace Spectrometre.Modules.Recrutement.Services;

/// <summary>Résultat d'analyse IA (ou repli local) pour une candidature.</summary>
public sealed record AnalyseIaView(
    string AnalyseTexte,
    DateTimeOffset GenereeLe,
    bool GenereeParIa,
    string? AvertissementIa = null);

/// <summary>
/// Guides 2ème entrevue et analyses IA — données du module Recrutement, séparées de
/// <c>IPosteService</c> (ProfilEntreprise) pour éviter une dépendance circulaire.
/// </summary>
public interface IRecrutementEntretienService : IRecrutementEntretienCleanup
{
    /// <summary>
    /// Guide de 2ème entrevue du poste (tenant actif). Null si le poste est introuvable ;
    /// instance vide (PosteId renseigné) s'il n'existe pas encore de ligne en base.
    /// </summary>
    Task<GuideDeuxiemeEntrevue?> GetGuideDeuxiemeEntrevueAsync(int posteId, CancellationToken cancellationToken = default);

    /// <summary>Upsert du guide pour le poste — no-op si le poste n'existe pas dans le tenant actif.</summary>
    Task SaveGuideDeuxiemeEntrevueAsync(int posteId, GuideDeuxiemeEntrevue guide, CancellationToken cancellationToken = default);

    /// <summary>Analyse IA déjà en cache pour la candidature, ou null.</summary>
    Task<AnalyseIaView?> GetAnalyseIaAsync(int candidatureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Génère (ou renvoie le cache si le hash est inchangé) l'analyse IA poste/candidature.
    /// Jamais d'exception remontée : en cas d'échec IA, texte de repli local (<see cref="AnalyseIaView.GenereeParIa"/> = false).
    /// </summary>
    Task<AnalyseIaView> GenererAnalyseIaAsync(int candidatureId, bool forcerRegeneration = false, CancellationToken cancellationToken = default);
}
