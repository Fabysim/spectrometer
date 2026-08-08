namespace Spectrometre.Core.Recruitment;

/// <summary>
/// Nettoyage des données d'assistance à l'entretien (guides 2ème entrevue, analyses IA) lors de
/// la suppression d'un poste ou d'une candidature. Implémenté par le module Recrutement ;
/// consommé par <c>IPosteService</c> (ProfilEntreprise) pour éviter une référence de projet
/// ProfilEntreprise → Recrutement.
/// </summary>
public interface IRecrutementEntretienCleanup
{
    /// <summary>Supprime guides et analyses liés au poste dans le schéma tenant ambiant.</summary>
    Task DeleteDonneesEntretienPourPosteAsync(int posteId, CancellationToken cancellationToken = default);

    /// <summary>Supprime l'analyse IA liée à la candidature dans le schéma tenant ambiant.</summary>
    Task DeleteDonneesEntretienPourCandidatureAsync(int candidatureId, CancellationToken cancellationToken = default);
}
