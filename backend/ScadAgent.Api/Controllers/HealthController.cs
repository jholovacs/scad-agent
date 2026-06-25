using Microsoft.AspNetCore.Mvc;
using ScadAgent.Application.DTOs;
using ScadAgent.Application.Interfaces;

namespace ScadAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IOllamaService _ollama;
    private readonly IOpenScadService _openScad;

    public HealthController(IOllamaService ollama, IOpenScadService openScad)
    {
        _ollama = ollama;
        _openScad = openScad;
    }

    [HttpGet]
    public async Task<ActionResult<HealthDto>> Get(CancellationToken cancellationToken)
    {
        string? ollamaError = null;
        var ollamaReachable = false;
        try
        {
            ollamaReachable = await _ollama.IsReachableAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            ollamaError = ex.Message;
        }

        string? openScadError = null;
        var openScadAvailable = false;
        try
        {
            openScadAvailable = _openScad.IsAvailable();
            if (!openScadAvailable)
                openScadError = "OpenSCAD is not available.";
        }
        catch (Exception ex)
        {
            openScadError = ex.Message;
        }

        return Ok(new HealthDto(
            true,
            ollamaReachable,
            ollamaReachable ? null : ollamaError ?? "Ollama unreachable",
            openScadAvailable,
            openScadAvailable ? null : openScadError));
    }
}
