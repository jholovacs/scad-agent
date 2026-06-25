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
        """;

    private readonly AgentOptions _options;

    public ContextManager(Microsoft.Extensions.Options.IOptions<AgentOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<OllamaMessage> BuildMessages(DesignContext context)
    {
        var messages = new List<OllamaMessage>
        {
            new("system", SystemPrompt)
        };

        var recent = context.RecentMessages
            .TakeLast(_options.MaxContextMessages)
            .Select(m => new OllamaMessage(m.Role, m.Content));

        messages.AddRange(recent);

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

        messages.Add(new OllamaMessage("user", context.UserInstruction));
        return messages;
    }
}
