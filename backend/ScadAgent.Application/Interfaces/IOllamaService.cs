namespace ScadAgent.Application.Interfaces;

public interface IOllamaService
{
    Task<string> ChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken cancellationToken = default);
    Task<bool> IsReachableAsync(CancellationToken cancellationToken = default);
}

public record OllamaMessage(string Role, string Content);
