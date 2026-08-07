namespace Spectrometre.Modules.ProfilCoach.Services;

public sealed record CoachProfileView(int Id, string UserId, string NomAffiche, string BioCourte, string Specialites, bool VisibleDansAnnuaire);

/// <summary>Vue minimale exposée par l'annuaire — jamais l'email/UserId, uniquement ce que le coach a choisi de rendre public.</summary>
public sealed record CoachAnnuaireEntry(int CoachProfileId, string NomAffiche, string BioCourte, string Specialites);

/// <summary>
/// Point d'entrée public du module Profil Coach. Le module Coaching passe exclusivement par cette
/// interface — jamais d'accès direct à <c>ProfilCoachDbContext</c> depuis l'extérieur du module (même
/// discipline que <c>ICandidateProfileService</c>/<c>ICompanyProfileService</c>).
/// </summary>
public interface ICoachProfileService
{
    Task<int> GetOrCreateProfileIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<CoachProfileView?> GetProfilAsync(string userId, CancellationToken cancellationToken = default);

    Task SaveProfilAsync(string userId, string nomAffiche, string bioCourte, string specialites, bool visibleDansAnnuaire, CancellationToken cancellationToken = default);

    /// <summary>Coachs ayant opté pour la visibilité, filtrés par un terme de recherche libre (nom affiché/spécialités) — utilisé par « Mon coach » côté personne suivie.</summary>
    Task<IReadOnlyList<CoachAnnuaireEntry>> GetAnnuaireVisibleAsync(string? recherche, CancellationToken cancellationToken = default);

    /// <summary>Résout le UserId propriétaire d'un profil coach — utilisé par le module Coaching pour retrouver le compte à contacter à partir d'une entrée d'annuaire.</summary>
    Task<string?> GetUserIdAsync(int coachProfileId, CancellationToken cancellationToken = default);
}
