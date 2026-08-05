namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Une entreprise cliente = un tenant = un schéma Postgres dédié (<see cref="SchemaName"/>),
/// sur le même principe que <c>co_*</c> en V1.
/// </summary>
public sealed class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>Nom du schéma Postgres qui porte les données métier de cette entreprise (ex. <c>co_atelier_nordik</c>).</summary>
    public required string SchemaName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UserCompanyLink> UserLinks { get; set; } = new List<UserCompanyLink>();
}
