namespace ScadAgent.Domain.ValueObjects;

public sealed class RenderResult
{
    public bool Success { get; init; }
    public string? StdOut { get; init; }
    public string? StdErr { get; init; }
    public string? StlPath { get; init; }
    public string? PreviewPath { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];

    public static RenderResult Succeeded(string stlPath, string? previewPath, TimeSpan duration, string? stdOut = null) =>
        new()
        {
            Success = true,
            StlPath = stlPath,
            PreviewPath = previewPath,
            Duration = duration,
            StdOut = stdOut
        };

    public static RenderResult Failed(string stdErr, TimeSpan duration, IReadOnlyList<ValidationIssue>? issues = null) =>
        new()
        {
            Success = false,
            StdErr = stdErr,
            Duration = duration,
            Issues = issues ?? ParseIssues(stdErr)
        };

    public static IReadOnlyList<ValidationIssue> ParseIssues(string? stdErr)
    {
        if (string.IsNullOrWhiteSpace(stdErr))
            return [];

        return stdErr
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => new ValidationIssue(line))
            .ToList();
    }
}
