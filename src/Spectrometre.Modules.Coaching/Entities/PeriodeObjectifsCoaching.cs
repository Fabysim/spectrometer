namespace Spectrometre.Modules.Coaching.Entities;

/// <summary>
/// Période d'objectifs de coaching rattachée à un <see cref="LienCoaching"/> — indépendante de tout
/// emploi / <c>UserCompanyLink</c> / poste. Une seule période non archivée à la fois par lien.
/// </summary>
public sealed class PeriodeObjectifsCoaching
{
    public int Id { get; set; }

    public int LienCoachingId { get; set; }

    public DateOnly DateDebut { get; set; }

    public DateOnly DateFin { get; set; }

    public bool Archivee { get; set; }

    public ICollection<ObjectifCoaching> Objectifs { get; set; } = [];
}
