using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Invalide le cache de modèle EF par schéma tenant, pour n'importe quel DbContext de module
/// qui implémente <see cref="ITenantScopedDbContext"/> — équivalent générique du
/// <c>TenantSchemaModelCacheKeyFactory</c> de V1, réutilisable par tous les modules sans modification.
/// </summary>
public sealed class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is ITenantScopedDbContext tenantScoped
            ? (context.GetType(), tenantScoped.TenantSchema, designTime)
            : (object)(context.GetType(), designTime);
}
