namespace Spectrometre.Modules.Compatibilite.Entities;

/// <summary>Les 5 axes définis dans la section K du document source (« Grille complémentaire des critères pour le moteur de compatibilité »).</summary>
public enum CompatibilityAxis
{
    Technique = 0,
    Comportementale = 1,
    Culturelle = 2,
    Organisationnelle = 3,
    Motivationnelle = 4,
}

public static class CompatibilityAxisLabels
{
    public static string Label(CompatibilityAxis axis) => axis switch
    {
        CompatibilityAxis.Technique => "Technique",
        CompatibilityAxis.Comportementale => "Comportementale",
        CompatibilityAxis.Culturelle => "Culturelle",
        CompatibilityAxis.Organisationnelle => "Organisationnelle",
        CompatibilityAxis.Motivationnelle => "Motivationnelle",
        _ => axis.ToString(),
    };
}
