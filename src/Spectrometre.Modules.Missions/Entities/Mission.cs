namespace Spectrometre.Modules.Missions.Entities;

public sealed class Mission
{
    public int Id { get; set; }
    public int ParticulierProfileId { get; set; }

    public MissionCategorie Categorie { get; set; }

    /// <summary>
    /// Précision libre. Obligatoire uniquement si <see cref="Categorie"/> == <see cref="MissionCategorie.Autre"/> ;
    /// sinon complément optionnel (pas un remplacement total du libellé de catégorie).
    /// </summary>
    public string Titre { get; set; } = "";

    public required string Description { get; set; }
    public string? Lieu { get; set; }
    /// <summary>Durée estimée (texte libre — ex. « 2 h », « une demi-journée »).</summary>
    public string? DureeEstimee { get; set; }
    public MissionDifficulte Difficulte { get; set; }
    public decimal? RemunerationMontant { get; set; }
    public string? CompetencesTravaillees { get; set; }

    public MissionNiveauEncadrement NiveauEncadrement { get; set; }

    /// <summary>Conditions pratiques / sécurité — questions factuelles, pas une enquête privée.</summary>
    public bool PresenceEscaliers { get; set; }
    public bool PresenceAnimaux { get; set; }
    public bool PortDeCharge { get; set; }
    public bool AccesDifficile { get; set; }
    public string? RisqueParticulier { get; set; }

    public MissionStatut Statut { get; set; } = MissionStatut.Disponible;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<MissionAcceptation> Acceptations { get; set; } = [];
}
