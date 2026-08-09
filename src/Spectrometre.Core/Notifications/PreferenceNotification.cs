namespace Spectrometre.Core.Notifications;

/// <summary>
/// Préférence opt-out par catégorie de notification. Sans ligne en base → catégorie active (défaut).
/// <see cref="CategorieCode"/> = préfixe de <c>TypeCode</c> avant le point
/// (ex. <c>Coaching</c> pour <c>Coaching.DemandeRecue</c>).
/// </summary>
public sealed class PreferenceNotification
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public required string CategorieCode { get; set; }

    public bool Active { get; set; } = true;
}
