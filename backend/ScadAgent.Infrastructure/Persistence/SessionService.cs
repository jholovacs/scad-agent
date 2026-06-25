using Microsoft.EntityFrameworkCore;
using ScadAgent.Application.DTOs;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Services;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Infrastructure.Persistence;

public class SessionService : ISessionService
{
    private readonly ScadAgentDbContext _db;

    public SessionService(ScadAgentDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SessionSummaryDto>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _db.Sessions.AsNoTracking().ToListAsync(cancellationToken);
        return sessions
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new SessionSummaryDto(s.Id, s.Title, s.Status, s.UpdatedAt))
            .ToList();
    }

    public async Task<SessionDetailDto> CreateSessionAsync(string? title, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new DesignSession
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled design" : title.Trim(),
            Status = SessionStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDetail(session);
    }

    public async Task<SessionDetailDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await LoadSessionGraphAsync(sessionId, cancellationToken);
        return session is null ? null : SessionMapper.ToDetail(session);
    }

    public async Task<DesignSession?> GetSessionEntityAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        await LoadSessionGraphAsync(sessionId, cancellationToken);

    public Task<ConversationMessage> AddUserMessageAsync(Guid sessionId, string content, CancellationToken cancellationToken = default) =>
        AddMessageAsync(sessionId, MessageRole.User, content, cancellationToken);

    public async Task<ConversationMessage> AddMessageAsync(
        Guid sessionId,
        MessageRole role,
        string content,
        CancellationToken cancellationToken = default)
    {
        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Messages.Add(message);

        var session = await _db.Sessions.FindAsync([sessionId], cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<DesignIteration> AddIterationAsync(DesignIteration iteration, CancellationToken cancellationToken = default)
    {
        _db.Iterations.Add(iteration);
        await _db.SaveChangesAsync(cancellationToken);
        return iteration;
    }

    public async Task UpdateIterationAsync(DesignIteration iteration, CancellationToken cancellationToken = default)
    {
        _db.Iterations.Update(iteration);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSessionAsync(DesignSession session, CancellationToken cancellationToken = default)
    {
        _db.Sessions.Update(session);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IterationDto>> GetIterationsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.Iterations
            .Where(i => i.SessionId == sessionId)
            .OrderBy(i => i.Version)
            .Select(i => new IterationDto(
                i.Id,
                i.Version,
                i.Status,
                i.ScadContent,
                i.AssistantSummary,
                i.RenderError,
                i.DiagnosticLog,
                !string.IsNullOrEmpty(i.StlArtifactPath),
                !string.IsNullOrEmpty(i.PreviewArtifactPath),
                i.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DesignIteration?> GetIterationAsync(Guid iterationId, CancellationToken cancellationToken = default) =>
        await _db.Iterations.FindAsync([iterationId], cancellationToken);

    private async Task<DesignSession?> LoadSessionGraphAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _db.Sessions
            .Include(s => s.Messages)
            .Include(s => s.Iterations)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }
}
