namespace Spectrometre.Modules.GestionDuTemps.Helpers;

/// <summary>Utilitaires de manipulation de couleurs hex pour le calendrier Organisation (porté depuis mvp).</summary>
public static class ColorHelper
{
    public static string Lighten(string hex, double factor)
    {
        var (r, g, b) = ParseHex(hex);
        factor = Math.Clamp(factor, 0, 1);
        return ToHex(
            (int)(r + (255 - r) * factor),
            (int)(g + (255 - g) * factor),
            (int)(b + (255 - b) * factor));
    }

    public static string Darken(string hex, double pct)
    {
        var (r, g, b) = ParseHex(hex);
        var factor = 1 - Math.Clamp(pct, 0, 100) / 100;
        return ToHex((int)(r * factor), (int)(g * factor), (int)(b * factor));
    }

    private static string ToHex(int r, int g, int b) =>
        $"#{Math.Clamp(r, 0, 255):X2}{Math.Clamp(g, 0, 255):X2}{Math.Clamp(b, 0, 255):X2}";

    private static (int R, int G, int B) ParseHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return (107, 82, 72); // #6b5248 fallback

        var value = hex.Trim().TrimStart('#');
        if (value.Length == 3)
            value = string.Concat(value.Select(c => $"{c}{c}"));

        if (value.Length != 6)
            return (107, 82, 72);

        return (
            Convert.ToInt32(value[..2], 16),
            Convert.ToInt32(value[2..4], 16),
            Convert.ToInt32(value[4..6], 16));
    }
}
