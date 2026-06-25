using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;

namespace ScadAgent.Application.Services;

public class ContextManager : IContextManager
{
    private const string SystemPrompt = """
        You are an expert OpenSCAD designer. Generate parametric, printable 3D models.

        Rules:
        - Respond with a single fenced code block using ```scad
        - Output only valid OpenSCAD code that renders without errors
        - Prefer simple primitives and clear variable names
        - Include brief comments for non-obvious geometry
        - When fixing errors, address every compiler message
        - Put `// @units mm` on the first line of every model unless the user explicitly requests inches (`// @units in`)
        - Use millimeters for all dimensions by default. OpenSCAD numbers have no unit metadata; STL files are interpreted as millimeters by slicers.
        - If using inches, state it in the @units directive and in comments so exports can be scaled correctly
        """;

    public IReadOnlyList<OllamaMessage> BuildMessages(DesignContext context)
    {
        var messages = new List<OllamaMessage>
        {
            new("system", SystemPrompt)
        };

        AppendConversationMemory(messages, context.ConversationSummary, context.Messages);

        if (!string.IsNullOrWhiteSpace(context.CurrentScad))
        {
            messages.Add(new OllamaMessage(
                "user",
                $"Current OpenSCAD source:\n```scad\n{context.CurrentScad}\n```"));
        }

        if (!string.IsNullOrWhiteSpace(context.LastRenderError))
        {
            messages.Add(new OllamaMessage(
                "user",
                $"The last render failed with these errors:\n{context.LastRenderError}\nFix the code."));
        }

        AppendUserTurnIfNeeded(messages, context.UserInstruction);
        return messages;
    }

    public IReadOnlyList<OllamaMessage> BuildConversationMessages(ConversationContext context)
    {
        const string conversationSystemPrompt = """
            You are an expert OpenSCAD and 3D design assistant helping a user refine a parametric model.

            Rules:
            - Answer questions clearly in plain text.
            - Explain geometry, parameters, OpenSCAD syntax, and design trade-offs when asked.
            - Do NOT output a new ```scad code block unless the user explicitly asks you to change or regenerate the design.
            - If the user only wants an explanation, provide text only.
            """;

        var messages = new List<OllamaMessage>
        {
            new("system", conversationSystemPrompt)
        };

        AppendConversationMemory(messages, context.ConversationSummary, context.Messages);

        if (!string.IsNullOrWhiteSpace(context.CurrentScad))
        {
            messages.Add(new OllamaMessage(
                "user",
                $"Current OpenSCAD source for reference:\n```scad\n{context.CurrentScad}\n```"));
        }

        AppendUserTurnIfNeeded(messages, context.UserQuestion);
        return messages;
    }

    private static void AppendConversationMemory(
        ICollection<OllamaMessage> messages,
        string? summary,
        IReadOnlyList<ConversationMessageContext> history)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            messages.Add(new OllamaMessage(
                "user",
                $"Session memory from earlier conversation:\n{summary.Trim()}"));
        }

        foreach (var message in history)
            messages.Add(new OllamaMessage(message.Role, message.Content));
    }

    private static void AppendUserTurnIfNeeded(ICollection<OllamaMessage> messages, string userText)
    {
        var trimmed = userText.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        if (messages.LastOrDefault() is { Role: "user", Content: var lastContent }
            && string.Equals(lastContent.Trim(), trimmed, StringComparison.Ordinal))
            return;

        messages.Add(new OllamaMessage("user", trimmed));
    }
}
