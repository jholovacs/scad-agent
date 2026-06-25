using ScadAgent.Application.DTOs;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Interfaces;

public interface ISessionService
{
    Task<IReadOnlyList<SessionSummaryDto>> ListSessionsAsync(CancellationToken cancellationToken = default);
    Task<SessionDetailDto> CreateSessionAsync(string? title, CancellationToken cancellationToken = default);
    Task<SessionDetailDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<DesignSession?> GetSessionEntityAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<ConversationMessage> AddUserMessageAsync(Guid sessionId, string content, CancellationToken cancellationToken = default);
    Task<ConversationMessage> AddMessageAsync(Guid sessionId, MessageRole role, string content, CancellationToken cancellationToken = default);
    Task<DesignIteration> AddIterationAsync(DesignIteration iteration, CancellationToken cancellationToken = default);
    Task UpdateIterationAsync(DesignIteration iteration, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(DesignSession session, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IterationDto>> GetIterationsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<DesignIteration?> GetIterationAsync(Guid iterationId, CancellationToken cancellationToken = default);
}
