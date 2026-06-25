using ScadAgent.Application.Interfaces;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Services;

public class MessageIntentClassifier : IMessageIntentClassifier
{
    private const string SystemPrompt = """
        You classify user messages in an OpenSCAD design assistant chat.
        Reply with exactly one word on the first line: DESIGN or QUESTION.

        DESIGN — the user wants to create, change, fix, or regenerate the 3D model or OpenSCAD code.
        QUESTION — the user wants an explanation, dimensions, advice, or discussion without changing the model.

        If the user asks what something is or how it works, that is QUESTION.
        If the user asks to make, add, remove, resize, or update geometry, that is DESIGN.
        When unsure, prefer DESIGN if they want the model changed; otherwise QUESTION.
        """;

    private readonly IOllamaService _ollama;

    public MessageIntentClassifier(IOllamaService ollama)
    {
        _ollama = ollama;
    }

    public async Task<MessageIntent> ClassifyAsync(
        string content,
        bool hasExistingDesign,
        CancellationToken cancellationToken = default)
    {
        var response = await _ollama.ChatAsync(
        [
            new OllamaMessage("system", SystemPrompt),
            new OllamaMessage("user", BuildUserPrompt(content, hasExistingDesign))
        ],
        cancellationToken);

        return ParseIntent(response);
    }

    public static MessageIntent ParseIntent(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return MessageIntent.Design;

        var firstLine = response.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]
            .Trim()
            .ToUpperInvariant();

        if (firstLine.Contains("QUESTION", StringComparison.Ordinal))
            return MessageIntent.Ask;

        return MessageIntent.Design;
    }

    private static string BuildUserPrompt(string content, bool hasExistingDesign) =>
        $"""
        Session already has a successful design: {(hasExistingDesign ? "yes" : "no")}

        User message:
        {content.Trim()}
        """;
}
