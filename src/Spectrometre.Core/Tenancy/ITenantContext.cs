namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Contexte tenant courant (scoped par requête / circuit Blazor) : quelle entreprise est active
/// pour l'utilisateur connecté, et donc quel schéma Postgres les DbContext de modules doivent cibler.
/// </summary>
public interface ITenantContext
{
    int? ActiveCompanyId { get; }

    /// <summary>Schéma Postgres de l'entreprise active. <c>"public"</c> tant qu'aucune entreprise n'est sélectionnée.</summary>
    string SchemaName { get; }

    void SetActiveCompany(int companyId, string schemaName);
}

public sealed class TenantContext : ITenantContext
{
    public int? ActiveCompanyId { get; private set; }
    public string SchemaName { get; private set; } = "public";

    public void SetActiveCompany(int companyId, string schemaName)
    {
        ActiveCompanyId = companyId;
        SchemaName = schemaName;
    }
}
