namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Table de liaison utilisateur ↔ entreprises : un compte peut posséder ou gérer plusieurs entreprises
/// (schéma partagé <c>core</c>, contrairement aux données métier qui vivent dans le schéma de chaque entreprise).
/// </summary>
public sealed class UserCompanyLink
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public CompanyRole Role { get; set; } = CompanyRole.Proprietaire;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
