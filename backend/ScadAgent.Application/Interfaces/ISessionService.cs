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
    Task<ConversationMessage> AddUserMessageAsync(
        Guid sessionId,
        string content,
        MessageIntent intent = MessageIntent.Design,
        CancellationToken cancellationToken = default);
    Task<ConversationMessage> AddMessageAsync(
        Guid sessionId,
        MessageRole role,
        string content,
        Guid? iterationId = null,
        MessageIntent? intent = null,
        CancellationToken cancellationToken = default);
    Task LinkMessageToIterationAsync(
        Guid messageId,
        Guid iterationId,
        CancellationToken cancellationToken = default);
    Task<DesignIteration> AddIterationAsync(DesignIteration iteration, CancellationToken cancellationToken = default);
    Task UpdateIterationAsync(DesignIteration iteration, CancellationToken cancellationToken = default);
    Task UpdateSessionAsync(DesignSession session, CancellationToken cancellationToken = default);
    Task<SessionDetailDto?> UpdateSessionTitleAsync(
        Guid sessionId,
        string title,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IterationsPageDto> GetIterationsPageAsync(
        Guid sessionId,
        int limit,
        int? beforeVersion = null,
        CancellationToken cancellationToken = default);
    Task<DesignIteration?> GetIterationAsync(Guid iterationId, CancellationToken cancellationToken = default);
    Task<MessagesPageDto> GetMessagesAsync(
        Guid sessionId,
        int limit,
        DateTimeOffset? before,
        Guid? iterationId = null,
        CancellationToken cancellationToken = default);
}
