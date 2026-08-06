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

    /// <summary>Traduction automatique pour l'instant (bilinguisme, cycle contenu métier) — à affiner plus tard.</summary>
    public static string LabelEn(CompatibilityAxis axis) => axis switch
    {
        CompatibilityAxis.Technique => "Technical",
        CompatibilityAxis.Comportementale => "Behavioral",
        CompatibilityAxis.Culturelle => "Cultural",
        CompatibilityAxis.Organisationnelle => "Organizational",
        CompatibilityAxis.Motivationnelle => "Motivational",
        _ => axis.ToString(),
    };

    /// <summary>Libellé de l'axe selon la culture — voir <see cref="Label(CompatibilityAxis)"/>/<see cref="LabelEn"/>.</summary>
    public static string Label(CompatibilityAxis axis, bool english) => english ? LabelEn(axis) : Label(axis);
}
