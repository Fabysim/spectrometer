using System.Globalization;

namespace Spectrometre.Modules.Missions;

/// <summary>
/// Montants missions en EUR — cultures neutres <c>fr</c>/<c>en</c> du Host n'ont pas de symbole monétaire
/// (<c>¤</c> avec <see cref="CultureInfo.CurrentCulture"/>), d'où fr-FR / en-IE explicites.
/// </summary>
internal static class MissionAmountFormat
{
    private static readonly CultureInfo FrEuro = CultureInfo.GetCultureInfo("fr-FR");
    private static readonly CultureInfo EnEuro = CultureInfo.GetCultureInfo("en-IE");

    public static string Format(decimal amount) =>
        amount.ToString("C", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? FrEuro : EnEuro);
}
