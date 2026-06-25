using ScadAgent.Application.DTOs;
using ScadAgent.Application.Interfaces;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;
using ScadAgent.Domain.ValueObjects;

namespace ScadAgent.Application.Services;

public static class SessionMapper
{
    public static SessionSummaryDto ToSummary(DesignSession session) =>
        new(session.Id, session.Title, session.Status, session.UpdatedAt);

    public static IterationDto ToDto(DesignIteration iteration) =>
        new(
            iteration.Id,
            iteration.Version,
            iteration.Status,
            iteration.ScadContent,
            iteration.AssistantSummary,
            iteration.Summary,
            iteration.RenderError,
            iteration.DiagnosticLog,
            !string.IsNullOrEmpty(iteration.StlArtifactPath),
            !string.IsNullOrEmpty(iteration.PreviewArtifactPath),
            iteration.CreatedAt,
            iteration.ScadUnits,
            iteration.StlExportUnits);

    public static MessageDto ToDto(ConversationMessage message) =>
        new(message.Id, message.Role, message.Content, message.CreatedAt, message.IterationId, message.Intent);

    public static SessionDetailDto ToDetail(DesignSession session)
    {
        var current = session.Iterations.FirstOrDefault(i => i.Id == session.CurrentIterationId)
            ?? session.Iterations.OrderByDescending(i => i.Version).FirstOrDefault();

        return new SessionDetailDto(
            session.Id,
            session.Title,
            session.Status,
            session.CurrentIterationId,
            session.CreatedAt,
            session.UpdatedAt,
            current is null ? null : ToDto(current),
            []);
    }
}
