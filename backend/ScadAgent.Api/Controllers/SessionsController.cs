using Microsoft.AspNetCore.Mvc;
using ScadAgent.Application.DTOs;
using ScadAgent.Application.Interfaces;

namespace ScadAgent.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessions;
    private readonly IDesignAgentService _agent;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        ISessionService sessions,
        IDesignAgentService agent,
        ILogger<SessionsController> logger)
    {
        _sessions = sessions;
        _agent = agent;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SessionSummaryDto>>> List(CancellationToken cancellationToken) =>
        Ok(await _sessions.ListSessionsAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SessionDetailDto>> Create(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.CreateSessionAsync(request.Title, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = session.Id }, session);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SessionDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetSessionAsync(id, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpGet("{id:guid}/iterations")]
    public async Task<ActionResult<IReadOnlyList<IterationDto>>> GetIterations(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _sessions.GetIterationsAsync(id, cancellationToken));

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<SessionDetailDto>> PostMessage(
        Guid id,
        [FromBody] PostMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Message content is required.");

        var session = await _sessions.GetSessionAsync(id, cancellationToken);
        if (session is null)
            return NotFound();

        await _sessions.AddUserMessageAsync(id, request.Content, cancellationToken);

        try
        {
            await _agent.RunIterationAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected agent failure for session {SessionId}", id);
        }

        var updated = await _sessions.GetSessionAsync(id, cancellationToken);
        return Ok(updated);
    }
}
