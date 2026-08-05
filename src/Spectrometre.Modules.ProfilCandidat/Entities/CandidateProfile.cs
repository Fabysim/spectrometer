namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Profil d'un candidat. Rattaché à un utilisateur (<c>core.AspNetUsers</c>), pas à une entreprise :
/// un candidat n'est pas un tenant, il peut être mis en compatibilité avec plusieurs entreprises.
/// C'est pourquoi ce module vit dans un schéma fixe (<c>profil_candidat</c>) et non par-tenant.
/// </summary>
public sealed class CandidateProfile
{
    public int Id { get; set; }
    public required string UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
