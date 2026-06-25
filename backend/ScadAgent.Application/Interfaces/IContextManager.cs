using ScadAgent.Application.Interfaces;

namespace ScadAgent.Application.Interfaces;

public interface IContextManager
{
    IReadOnlyList<OllamaMessage> BuildMessages(DesignContext context);
}

public record DesignContext(
    string? CurrentScad,
    string UserInstruction,
    IReadOnlyList<ConversationMessageContext> RecentMessages,
    string? LastRenderError);

public record ConversationMessageContext(string Role, string Content);
