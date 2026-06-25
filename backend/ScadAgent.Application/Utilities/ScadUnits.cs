using System.Text;
using System.Text.RegularExpressions;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Utilities;

public static partial class ScadUnits
{
    public const double InchesToMillimeters = 25.4;

    public static LinearUnits Parse(string scad)
    {
        var directive = UnitsDirectiveRegex().Match(scad);
        if (directive.Success)
            return ParseToken(directive.Groups[1].Value);

        if (InchCommentRegex().IsMatch(scad))
            return LinearUnits.Inches;

        return LinearUnits.Millimeters;
    }

    public static string ForRender(string scad, LinearUnits units)
    {
        var body = StripUnitsDirective(scad).Trim();
        if (units == LinearUnits.Millimeters)
        {
            return string.IsNullOrWhiteSpace(body)
                ? "// @units mm"
                : EnsureUnitsDirective(body, LinearUnits.Millimeters);
        }

        var builder = new StringBuilder();
        builder.AppendLine("// @units mm");
        builder.AppendLine("// Rendered for STL export: inch-based source scaled by 25.4");
        builder.AppendLine("scale(25.4) {");
        foreach (var line in body.Split('\n'))
            builder.AppendLine(line);
        builder.AppendLine("}");
        return builder.ToString();
    }

    public static string ForScadDownload(string scad, LinearUnits units)
    {
        var builder = new StringBuilder();
        builder.AppendLine(ExportBanner(units));
        builder.Append(EnsureUnitsDirective(StripUnitsDirective(scad).Trim(), units));
        return builder.ToString().TrimEnd() + "\n";
    }

    public static string ExportBanner(LinearUnits scadUnits) =>
        scadUnits == LinearUnits.Inches
            ? "// SCAD Agent export: source dimensions are inches. STL downloads are millimeters (×25.4)."
            : "// SCAD Agent export: dimensions are millimeters. STL files use the same mm coordinates.";

    public static string UnitsLabel(LinearUnits units) =>
        units == LinearUnits.Inches ? "in" : "mm";

    public static string StlUnitsLabel() => "mm";

    private static string EnsureUnitsDirective(string scad, LinearUnits units)
    {
        var stripped = StripUnitsDirective(scad).Trim();
        return $"// @units {UnitsLabel(units)}\n{stripped}";
    }

    private static string StripUnitsDirective(string scad) =>
        UnitsDirectiveLineRegex().Replace(scad, string.Empty).TrimStart();

    private static LinearUnits ParseToken(string token) =>
        token.StartsWith("i", StringComparison.OrdinalIgnoreCase)
            ? LinearUnits.Inches
            : LinearUnits.Millimeters;

    [GeneratedRegex(@"^\s*//\s*@units\s*:?\s*(mm|millimeters?|in|inch|inches)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex UnitsDirectiveLineRegex();

    [GeneratedRegex(@"//\s*@units\s*:?\s*(mm|millimeters?|in|inch|inches)\b", RegexOptions.IgnoreCase)]
    private static partial Regex UnitsDirectiveRegex();

    [GeneratedRegex(@"//.*\binches?\b", RegexOptions.IgnoreCase)]
    private static partial Regex InchCommentRegex();
}
