namespace Spectrometre.Modules.Coaching.Entities;

/// <summary>
/// Période d'objectifs de coaching rattachée à un <see cref="LienCoaching"/> — indépendante de tout
/// emploi / <c>UserCompanyLink</c> / poste. Une seule période non archivée à la fois par lien.
/// Au transfert de coach, seule la période courante (<see cref="Archivee"/> false) change de
/// <see cref="LienCoachingId"/> ; les archives restent sur l'ancien lien (historique de relation,
/// listé par <c>GetArchivesAsync</c>). Les <see cref="ObjectifCoaching"/> suivent via
/// <see cref="ObjectifCoaching.PeriodeObjectifsCoachingId"/>.
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
