namespace URLBox.Domain.Entities;

public static class ProjectColor
{
    private static readonly string[] Palette = { "#2563EB", "#9333EA", "#15803D", "#C2410C", "#BE185D", "#0E7490", "#A16207", "#4F46E5" };

    public static bool IsValid(string color) => color.Length == 7 && color[0] == '#'
        && color.Skip(1).All(c => char.IsAsciiHexDigit(c));

    public static string Resolve(string? color, int id) => string.IsNullOrEmpty(color)
        ? Palette[(Math.Max(id, 1) - 1) % Palette.Length] : color.ToUpperInvariant();

    public static string Foreground(string color)
    {
        var r = Convert.ToInt32(color.Substring(1, 2), 16);
        var g = Convert.ToInt32(color.Substring(3, 2), 16);
        var b = Convert.ToInt32(color.Substring(5, 2), 16);
        double Linear(int value) => value / 255d <= .04045 ? value / 255d / 12.92 : Math.Pow((value / 255d + .055) / 1.055, 2.4);
        return .2126 * Linear(r) + .7152 * Linear(g) + .0722 * Linear(b) > .179 ? "#000000" : "#FFFFFF";
    }
}
