using System.Text.RegularExpressions;

namespace ScadAgent.Application.Services;

public static partial class ScadCodeExtractor
{
    public static string Extract(string llmResponse)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
            throw new InvalidOperationException("LLM returned empty response.");

        var fenced = ScadFenceRegex().Match(llmResponse);
        if (fenced.Success)
            return fenced.Groups["code"].Value.Trim();

        var generic = GenericFenceRegex().Match(llmResponse);
        if (generic.Success)
            return generic.Groups["code"].Value.Trim();

        if (llmResponse.Contains("cube(", StringComparison.Ordinal) ||
            llmResponse.Contains("cylinder(", StringComparison.Ordinal) ||
            llmResponse.Contains("sphere(", StringComparison.Ordinal) ||
            llmResponse.Contains("module ", StringComparison.Ordinal))
            return llmResponse.Trim();

        throw new InvalidOperationException("LLM response did not contain recognizable OpenSCAD code.");
    }

    [GeneratedRegex(@"```(?:scad|openscad)?\s*\n(?<code>[\s\S]*?)```", RegexOptions.IgnoreCase)]
    private static partial Regex ScadFenceRegex();

    [GeneratedRegex(@"```\s*\n(?<code>[\s\S]*?)```")]
    private static partial Regex GenericFenceRegex();
}
