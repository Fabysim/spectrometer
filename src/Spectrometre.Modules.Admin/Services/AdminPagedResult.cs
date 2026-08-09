namespace Spectrometre.Modules.Admin.Services;

/// <summary>Résultat paginé pour les listes Admin (Skip/Take côté service, pas seulement UI).</summary>
public sealed record AdminPagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public int FromIndex => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int ToIndex => Math.Min(Page * PageSize, TotalCount);
}

/// <summary>Normalisation commune des paramètres de pagination Admin (défaut 20, max 100).</summary>
public static class AdminPaging
{
    public static readonly int[] AllowedPageSizes = [10, 20, 50];

    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        page = Math.Max(1, page);
        if (pageSize < 1)
            pageSize = DefaultPageSize;
        else if (pageSize > MaxPageSize)
            pageSize = MaxPageSize;
        return (page, pageSize);
    }
}
