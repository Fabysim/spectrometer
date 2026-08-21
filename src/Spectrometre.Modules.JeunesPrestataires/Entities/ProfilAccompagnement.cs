namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Profil d'accompagnement choisi par le coach à l'invitation.
/// <see cref="SansExperience"/> est la valeur par défaut : en cas de doute, on commence
/// par les micro-tâches concrètes (la plupart des jeunes invités sont mineurs sans expérience).
/// </summary>
public enum ProfilAccompagnement
{
    SansExperience = 0,
    Autonome = 1,
}
