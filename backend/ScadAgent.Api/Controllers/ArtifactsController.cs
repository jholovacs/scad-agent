using Microsoft.AspNetCore.Mvc;
using ScadAgent.Application.Interfaces;

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
        if (iteration is null || string.IsNullOrEmpty(iteration.StlArtifactPath))
            return NotFound();

        var stream = await _artifacts.OpenStlAsync(iteration.StlArtifactPath, cancellationToken);
        if (stream is null)
            return NotFound();

        return File(stream, "model/stl", $"iteration-{id:N}.stl");
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
