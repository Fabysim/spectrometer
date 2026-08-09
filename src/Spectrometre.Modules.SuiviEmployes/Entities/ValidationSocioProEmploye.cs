namespace Spectrometre.Modules.SuiviEmployes.Entities;

/// <summary>Verrouillage initial socio-pro (équivalent mvp <c>ManagerSocioProValidation</c>).</summary>
public sealed class ValidationSocioProEmploye
{
    public int Id { get; set; }

    public int UserCompanyLinkId { get; set; }

    public int PosteId { get; set; }

    public DateTimeOffset ValidatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
