namespace Spectrometre.Modules.Missions.Entities;

public enum MissionStatut
{
    Disponible = 0,
    EnAttenteValidation = 1,
    Attribuee = 2,
    Terminee = 3,
    Annulee = 4,
    /// <summary>
    /// File de modération association/coach, avant <see cref="Disponible"/>.
    /// Valeur en fin d'enum pour ne pas renuméroter les statuts déjà persistés (0–4).
    /// </summary>
    EnAttenteModeration = 5,
}

public enum MissionDifficulte
{
    Facile = 0,
    Intermediaire = 1,
    Exigeante = 2,
}

/// <summary>Catégorie de tâche guidée à la publication (remplace un titre libre seul).</summary>
public enum MissionCategorie
{
    JardinageSimple = 0,
    Rangement = 1,
    NettoyageLeger = 2,
    AideDemenagementLeger = 3,
    PetitBricolageNonDangereux = 4,
    AideLogistique = 5,
    TriClassementOrganisation = 6,
    AccompagnementTachePratique = 7,
    SoinsAuxAnimaux = 8,
    LavageDeVoiture = 9,
    Autre = 10,
}

/// <summary>Niveau d'encadrement souhaité par le particulier pendant la mission.</summary>
public enum MissionNiveauEncadrement
{
    PresentPendantMission = 0,
    PresentDebutSeulement = 1,
    AccompagnateurSouhaite = 2,
    Binome = 3,
    AutonomieApresExplication = 4,
}
