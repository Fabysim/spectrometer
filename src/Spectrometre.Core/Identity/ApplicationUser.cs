using Microsoft.AspNetCore.Identity;

namespace Spectrometre.Core.Identity;

/// <summary>Utilisateur applicatif. Un même utilisateur peut être rattaché à plusieurs entreprises (voir <see cref="Tenancy.UserCompanyLink"/>).</summary>
public sealed class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
