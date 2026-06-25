using Microsoft.AspNetCore.Mvc;
using ScadAgent.Application.DTOs;
using ScadAgent.Application.Interfaces;

namespace ScadAgent.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessions;
    private readonly ISessionMessageService _messages;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(
        ISessionService sessions,
        ISessionMessageService messages,
        ILogger<SessionsController> logger)
    {
        _sessions = sessions;
        _messages = messages;
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

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<SessionDetailDto>> Update(
        Guid id,
        [FromBody] UpdateSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest("Title is required.");

        var session = await _sessions.UpdateSessionTitleAsync(id, request.Title, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _sessions.DeleteSessionAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/iterations")]
    public async Task<ActionResult<IterationsPageDto>> GetIterations(
        Guid id,
        [FromQuery] int limit = 10,
        [FromQuery] int? beforeVersion = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetSessionAsync(id, cancellationToken);
        if (session is null)
            return NotFound();

        return Ok(await _sessions.GetIterationsPageAsync(id, limit, beforeVersion, cancellationToken));
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<MessagesPageDto>> GetMessages(
        Guid id,
        [FromQuery] int limit = 10,
        [FromQuery] DateTimeOffset? before = null,
        [FromQuery] Guid? iterationId = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetSessionAsync(id, cancellationToken);
        if (session is null)
            return NotFound();

        return Ok(await _sessions.GetMessagesAsync(id, limit, before, iterationId, cancellationToken));
    }

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

        try
        {
            await _messages.HandleAsync(id, request.Content, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected message handling failure for session {SessionId}", id);
        }

        var updated = await _sessions.GetSessionAsync(id, cancellationToken);
        return Ok(updated);
    }
}
