using ScadAgent.Domain.Enums;

namespace ScadAgent.Domain.Entities;

public class DesignIteration
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public int Version { get; set; }
    public string ScadContent { get; set; } = string.Empty;
    public string ScadHash { get; set; } = string.Empty;
    public IterationStatus Status { get; set; }
    public string? AssistantSummary { get; set; }
    public string? Summary { get; set; }
    public string? RenderError { get; set; }
    public string? DiagnosticLog { get; set; }
    public string? StlArtifactPath { get; set; }
    public string? PreviewArtifactPath { get; set; }
    public int CorrectionAttempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public LinearUnits ScadUnits { get; set; } = LinearUnits.Millimeters;
    public LinearUnits StlExportUnits { get; set; } = LinearUnits.Millimeters;

    public DesignSession? Session { get; set; }

    public void MarkRendering()
    {
        Status = IterationStatus.Rendering;
    }

    public void MarkSucceeded(string stlPath, string? previewPath, string? summary)
    {
        if (string.IsNullOrWhiteSpace(stlPath))
            throw new InvalidOperationException("Cannot mark iteration succeeded without an STL artifact.");

        Status = IterationStatus.Succeeded;
        StlArtifactPath = stlPath;
        PreviewArtifactPath = previewPath;
        Summary = summary;
        RenderError = null;
    }

    public void MarkFailed(string error)
    {
        Status = IterationStatus.Failed;
        RenderError = error;
        StlArtifactPath = null;
        PreviewArtifactPath = null;
    }
}
