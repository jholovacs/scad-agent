using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Interfaces;

public interface IMessageIntentClassifier
{
    Task<MessageIntent> ClassifyAsync(
        string content,
        bool hasExistingDesign,
        CancellationToken cancellationToken = default);
}
