namespace Spectrometre.Modules.Entretien.Services;

/// <summary>Un groupe de questions ciblées sur un axe faible ou un point de vigilance partagé — deux colonnes, un sens chacune.</summary>
public sealed record QuestionGroupeView(
    string Titre,
    int? Score,
    IReadOnlyList<string> QuestionsPourCandidat,
    IReadOnlyList<string> QuestionsPourEntreprise);

public sealed record GrilleEntretienView(
    int CandidateProfileId,
    int ScoreGlobal,
    IReadOnlyList<QuestionGroupeView> Groupes);

/// <summary>
/// Point d'entrée public du module Entretien. Ne prend jamais de raccourci d'accès : la seule façon
/// d'obtenir une grille est de passer par <see cref="GenererGrilleAsync"/>, qui délègue intégralement le
/// contrôle d'accès à <c>ICompatibiliteService.GetResultatAutorisePourUtilisateurAsync</c> — exactement
/// le même accesseur sécurisé que <c>/compatibilite/resultat/{id}</c>, pour ne jamais réintroduire la
/// faille corrigée sur cette page (accès à un résultat par simple identifiant, sans candidature réelle).
/// </summary>
public interface IEntretienService
{
    /// <summary>
    /// Génère la grille de questions pour ce candidat, du point de vue de <paramref name="requestingUserId"/>.
    /// Retourne <c>null</c> si l'accès n'est pas autorisé (candidat inexistant, ou ni le candidat concerné,
    /// ni un gestionnaire d'une entreprise avec une candidature réelle pour ce candidat) — jamais une
    /// exception qui confirmerait l'existence de la ressource à un tiers.
    /// </summary>
    Task<GrilleEntretienView?> GenererGrilleAsync(int candidateProfileId, string requestingUserId, CancellationToken cancellationToken = default);
}
