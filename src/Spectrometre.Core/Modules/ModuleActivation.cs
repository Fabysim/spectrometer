namespace Spectrometre.Core.Modules;

/// <summary>Trace, pour une entreprise donnée, quels modules sont activés (table de registre, schéma <c>core</c>).</summary>
public sealed class ModuleActivation
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public required string ModuleCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.UtcNow;
}
