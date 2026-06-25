using ScadAgent.Application.Interfaces;

namespace ScadAgent.Application.Interfaces;

public interface IContextManager
{
    IReadOnlyList<OllamaMessage> BuildMessages(DesignContext context);
    IReadOnlyList<OllamaMessage> BuildConversationMessages(ConversationContext context);
}

public record DesignContext(
    string? CurrentScad,
    string UserInstruction,
    string? ConversationSummary,
    IReadOnlyList<ConversationMessageContext> Messages,
    string? LastRenderError);

public record ConversationContext(
    string? CurrentScad,
    string UserQuestion,
    string? ConversationSummary,
    IReadOnlyList<ConversationMessageContext> Messages);

public record ConversationMessageContext(string Role, string Content);
