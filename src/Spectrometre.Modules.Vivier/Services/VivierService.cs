using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilCandidat.Services;

namespace Spectrometre.Modules.Vivier.Services;

/// <summary>Voir <see cref="IVivierService"/> pour la contrainte de confidentialité que ce service applique.</summary>
public sealed class VivierService(
    IRecruitmentIndexService recruitmentIndex,
    ITenantContext tenantContext,
    ICandidateProfileService candidateProfileService) : IVivierService
{
    private int ActiveCompanyId =>
        tenantContext.ActiveCompanyId ?? throw new InvalidOperationException("Aucune entreprise active — le Vivier nécessite un tenant sélectionné.");

    public async Task<IReadOnlyList<CandidatureVivierView>> GetCandidaturesAsync(CancellationToken cancellationToken = default)
    {
        var entries = await recruitmentIndex.GetCandidaturesPourEntrepriseAsync(ActiveCompanyId, cancellationToken);
        return entries
            .Select(e => new CandidatureVivierView(e.PosteId, e.PosteTitre, e.CandidateProfileId, e.Statut, e.ScoreCompatibilite, e.TagsCles, e.UpdatedAt))
            .ToList();
    }

    public async Task<CandidateCompatibilityCriteriaView?> GetCandidateDetailAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        // Garde de confidentialité : on ne lit le détail du profil QUE si ce candidat a réellement postulé
        // à l'entreprise active — jamais un accès libre au module Profil Candidat par identifiant, y
        // compris si quelqu'un tape directement /vivier/candidat/{id} dans la barre d'adresse.
        var candidatures = await recruitmentIndex.GetCandidaturesPourEntrepriseAsync(ActiveCompanyId, cancellationToken);
        var aPostuleIci = candidatures.Any(c => c.CandidateProfileId == candidateProfileId);
        if (!aPostuleIci)
            return null;

        return await candidateProfileService.GetCompatibilityCriteriaAsync(candidateProfileId, cancellationToken);
    }
}
