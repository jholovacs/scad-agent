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
    private readonly IArtifactStore _artifacts;

    public SessionService(ScadAgentDbContext db, IArtifactStore artifacts)
    {
        _db = db;
        _artifacts = artifacts;
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
        await LoadSessionEntityGraphAsync(sessionId, cancellationToken);

    public Task<ConversationMessage> AddUserMessageAsync(
        Guid sessionId,
        string content,
        MessageIntent intent = MessageIntent.Design,
        CancellationToken cancellationToken = default) =>
        AddMessageAsync(sessionId, MessageRole.User, content, iterationId: null, intent, cancellationToken);

    public async Task<ConversationMessage> AddMessageAsync(
        Guid sessionId,
        MessageRole role,
        string content,
        Guid? iterationId = null,
        MessageIntent? intent = null,
        CancellationToken cancellationToken = default)
    {
        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            IterationId = iterationId,
            Role = role,
            Intent = role == MessageRole.User ? intent : null,
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

    public async Task LinkMessageToIterationAsync(
        Guid messageId,
        Guid iterationId,
        CancellationToken cancellationToken = default)
    {
        var message = await _db.Messages.FindAsync([messageId], cancellationToken)
            ?? throw new KeyNotFoundException($"Message {messageId} not found.");

        message.IterationId = iterationId;
        await _db.SaveChangesAsync(cancellationToken);
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

    public async Task<SessionDetailDto?> UpdateSessionTitleAsync(
        Guid sessionId,
        string title,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.Sessions.FindAsync([sessionId], cancellationToken);
        if (session is null)
            return null;

        session.Title = string.IsNullOrWhiteSpace(title) ? "Untitled design" : title.Trim();
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetSessionAsync(sessionId, cancellationToken);
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.Sessions.AnyAsync(s => s.Id == sessionId, cancellationToken);
        if (!exists)
            return false;

        await _db.Sessions
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.CurrentIterationId, (Guid?)null),
                cancellationToken);

        await _db.Messages.Where(m => m.SessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _db.Iterations.Where(i => i.SessionId == sessionId).ExecuteDeleteAsync(cancellationToken);
        await _db.Sessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync(cancellationToken);

        _artifacts.DeleteSessionArtifacts(sessionId);
        return true;
    }

    public async Task<IterationsPageDto> GetIterationsPageAsync(
        Guid sessionId,
        int limit,
        int? beforeVersion = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var candidates = await _db.Iterations
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        IEnumerable<DesignIteration> filtered = candidates;
        if (beforeVersion.HasValue)
            filtered = filtered.Where(i => i.Version < beforeVersion.Value);

        var rows = filtered
            .OrderByDescending(i => i.Version)
            .Take(limit + 1)
            .ToList();

        var hasMore = rows.Count > limit;
        if (hasMore)
            rows = rows.Take(limit).ToList();

        return new IterationsPageDto(
            rows.Select(SessionMapper.ToDto).ToList(),
            hasMore,
            rows.Count > 0 ? rows[^1].Version : null);
    }

    public async Task<DesignIteration?> GetIterationAsync(Guid iterationId, CancellationToken cancellationToken = default) =>
        await _db.Iterations.FindAsync([iterationId], cancellationToken);

    public async Task<MessagesPageDto> GetMessagesAsync(
        Guid sessionId,
        int limit,
        DateTimeOffset? before,
        Guid? iterationId = null,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var query = _db.Messages.AsNoTracking().Where(m => m.SessionId == sessionId);
        if (iterationId.HasValue)
            query = query.Where(m => m.IterationId == iterationId.Value);

        // SQLite cannot translate DateTimeOffset ordering or comparisons in SQL.
        var candidates = await query.ToListAsync(cancellationToken);

        IEnumerable<ConversationMessage> filtered = candidates;
        if (before.HasValue)
            filtered = filtered.Where(m => m.CreatedAt < before.Value);

        var rows = filtered
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit + 1)
            .ToList();

        var hasMore = rows.Count > limit;
        if (hasMore)
            rows = rows.Take(limit).ToList();

        return new MessagesPageDto(
            rows.Select(SessionMapper.ToDto).ToList(),
            hasMore,
            rows.Count > 0 ? rows[^1].CreatedAt : null);
    }

    private async Task<DesignSession?> LoadSessionGraphAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _db.Sessions
            .Include(s => s.Iterations)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    private async Task<DesignSession?> LoadSessionEntityGraphAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        return await _db.Sessions
            .Include(s => s.Messages)
            .Include(s => s.Iterations)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }
}
