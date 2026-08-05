namespace Spectrometre.Modules.Entretien.Entities;

/// <summary>
/// Réglages de génération, une seule ligne par entreprise — même principe éditable que
/// <c>Spectrometre.Modules.Compatibilite.Entities.CompatibilityWeightSetting</c> (poids des axes) :
/// modification directe en base pour l'instant, structuré pour un futur écran d'administration.
/// </summary>
public sealed class InterviewGenerationSettings
{
    public int Id { get; set; }

    /// <summary>Score en-dessous duquel un axe est considéré « faible » et génère des questions ciblées. Défaut 60, comme demandé.</summary>
    public int SeuilAxeFaiblePercent { get; set; } = 60;
}
