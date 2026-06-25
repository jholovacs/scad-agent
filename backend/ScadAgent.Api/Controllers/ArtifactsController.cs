using Microsoft.AspNetCore.Mvc;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Utilities;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Api.Controllers;

[ApiController]
[Route("api/iterations")]
public class ArtifactsController : ControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IArtifactStore _artifacts;

    public ArtifactsController(ISessionService sessions, IArtifactStore artifacts)
    {
        _sessions = sessions;
        _artifacts = artifacts;
    }

    [HttpGet("{id:guid}/artifacts/stl")]
    public async Task<IActionResult> GetStl(Guid id, CancellationToken cancellationToken)
    {
        var iteration = await _sessions.GetIterationAsync(id, cancellationToken);
        if (iteration is null || iteration.Status != IterationStatus.Succeeded)
            return NotFound();

        if (string.IsNullOrEmpty(iteration.StlArtifactPath))
            return NotFound();

        var stream = await _artifacts.OpenStlAsync(iteration.StlArtifactPath, cancellationToken);
        if (stream is null)
            return NotFound();

        var session = await _sessions.GetSessionAsync(iteration.SessionId, cancellationToken);
        var fileName = ExportFileName.ForIteration(session?.Title, iteration.Version, "stl", includeMillimeterSuffix: true);
        var scadUnits = iteration.ScadUnits;
        Response.Headers["X-Scad-Agent-Stl-Units"] = ScadUnits.StlUnitsLabel();
        Response.Headers["X-Scad-Agent-Scad-Units"] = ScadUnits.UnitsLabel(scadUnits);
        if (scadUnits == LinearUnits.Inches)
            Response.Headers["X-Scad-Agent-Stl-Scale-Note"] = "Converted from inch-based SCAD using scale(25.4)";

        return File(stream, "model/stl", fileName);
    }

    [HttpGet("{id:guid}/artifacts/scad")]
    public async Task<IActionResult> GetScad(Guid id, CancellationToken cancellationToken)
    {
        var iteration = await _sessions.GetIterationAsync(id, cancellationToken);
        if (iteration is null || iteration.Status != IterationStatus.Succeeded)
            return NotFound();

        if (string.IsNullOrWhiteSpace(iteration.ScadContent))
            return NotFound();

        var session = await _sessions.GetSessionAsync(iteration.SessionId, cancellationToken);
        var fileName = ExportFileName.ForIteration(session?.Title, iteration.Version, "scad");
        var scadUnits = ScadUnits.Parse(iteration.ScadContent);
        var bytes = System.Text.Encoding.UTF8.GetBytes(ScadUnits.ForScadDownload(iteration.ScadContent, scadUnits));
        Response.Headers["X-Scad-Agent-Scad-Units"] = ScadUnits.UnitsLabel(scadUnits);
        Response.Headers["X-Scad-Agent-Stl-Units"] = ScadUnits.StlUnitsLabel();
        return File(bytes, "application/octet-stream", fileName);
    }

    [HttpGet("{id:guid}/artifacts/preview")]
    public async Task<IActionResult> GetPreview(Guid id, CancellationToken cancellationToken)
    {
        var iteration = await _sessions.GetIterationAsync(id, cancellationToken);
        if (iteration is null || string.IsNullOrEmpty(iteration.PreviewArtifactPath))
            return NotFound();

        var stream = await _artifacts.OpenPreviewAsync(iteration.PreviewArtifactPath, cancellationToken);
        if (stream is null)
            return NotFound();

        return File(stream, "image/png");
    }
}
