using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.DTOs;

public record SessionSummaryDto(Guid Id, string Title, SessionStatus Status, DateTimeOffset UpdatedAt);

public record IterationDto(
    Guid Id,
    int Version,
    IterationStatus Status,
    string ScadContent,
    string? AssistantSummary,
    string? Summary,
    string? RenderError,
    string? DiagnosticLog,
    bool HasStl,
    bool HasPreview,
    DateTimeOffset CreatedAt,
    LinearUnits ScadUnits,
    LinearUnits StlExportUnits);

public record MessageDto(
    Guid Id,
    MessageRole Role,
    string Content,
    DateTimeOffset CreatedAt,
    Guid? IterationId,
    MessageIntent? Intent);

public record MessagesPageDto(
    IReadOnlyList<MessageDto> Messages,
    bool HasMore,
    DateTimeOffset? OldestCreatedAt);

public record IterationsPageDto(
    IReadOnlyList<IterationDto> Iterations,
    bool HasMore,
    int? OldestVersion);

public record SessionDetailDto(
    Guid Id,
    string Title,
    SessionStatus Status,
    Guid? CurrentIterationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IterationDto? CurrentIteration,
    IReadOnlyList<MessageDto> Messages);

public record CreateSessionRequest(string? Title);

public record UpdateSessionRequest(string Title);

public record PostMessageRequest(string Content);

public record HealthDto(
    bool ApiHealthy,
    bool OllamaReachable,
    string? OllamaError,
    bool OpenScadAvailable,
    string? OpenScadError);

public record AgentProgressDto(
    Guid SessionId,
    Guid? IterationId,
    string Phase,
    string Message,
    string? Details = null);
