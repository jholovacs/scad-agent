using System.Text.RegularExpressions;

namespace ScadAgent.Application.Utilities;

public static partial class ExportFileName
{
    public static string Sanitize(string? title, int maxLength = 80)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "design";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(title
            .Trim()
            .Select(character => invalid.Contains(character) ? '-' : character)
            .ToArray());

        cleaned = Whitespace().Replace(cleaned, " ").Trim('-', ' ', '.');
        if (string.IsNullOrWhiteSpace(cleaned))
            return "design";

        return cleaned.Length > maxLength ? cleaned[..maxLength].TrimEnd('-', ' ', '.') : cleaned;
    }

    public static string ForIteration(string? sessionTitle, int version, string extension) =>
        ForIteration(sessionTitle, version, extension, includeMillimeterSuffix: false);

    public static string ForIteration(string? sessionTitle, int version, string extension, bool includeMillimeterSuffix)
    {
        var baseName = Sanitize(sessionTitle);
        var ext = extension.TrimStart('.');
        var suffix = includeMillimeterSuffix && ext.Equals("stl", StringComparison.OrdinalIgnoreCase)
            ? "-mm"
            : string.Empty;
        return $"{baseName}-v{version}{suffix}.{ext}";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
