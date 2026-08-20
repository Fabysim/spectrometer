using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

/// <summary>Libellés serveur (notifications) — l'UI préfère les ressources localisées.</summary>
public static class MissionDisplay
{
    public static string TitreAffiche(MissionCategorie categorie, string? titrePrecision)
    {
        var precision = string.IsNullOrWhiteSpace(titrePrecision) ? null : titrePrecision.Trim();
        if (categorie == MissionCategorie.Autre)
            return precision ?? "Autre";

        var label = LabelFr(categorie);
        return precision is null ? label : $"{label} — {precision}";
    }

    public static string LabelFr(MissionCategorie categorie) => categorie switch
    {
        MissionCategorie.JardinageSimple => "Jardinage simple",
        MissionCategorie.Rangement => "Rangement",
        MissionCategorie.NettoyageLeger => "Nettoyage léger",
        MissionCategorie.AideDemenagementLeger => "Aide au déménagement léger",
        MissionCategorie.PetitBricolageNonDangereux => "Petit bricolage non dangereux",
        MissionCategorie.AideLogistique => "Aide logistique",
        MissionCategorie.TriClassementOrganisation => "Tri, classement, organisation",
        MissionCategorie.AccompagnementTachePratique => "Accompagnement d'une tâche pratique",
        MissionCategorie.SoinsAuxAnimaux => "Soins aux animaux",
        MissionCategorie.LavageDeVoiture => "Lavage de voiture",
        MissionCategorie.Autre => "Autre",
        _ => categorie.ToString(),
    };
}
