using ScadAgent.Domain.Enums;

namespace ScadAgent.Domain.Entities;

public class DesignSession
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public Guid? CurrentIterationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public DesignIteration? CurrentIteration { get; set; }
    public ICollection<DesignIteration> Iterations { get; set; } = new List<DesignIteration>();
    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

    public void BeginIteration()
    {
        Status = SessionStatus.Iterating;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkReady(Guid iterationId)
    {
        CurrentIterationId = iterationId;
        Status = SessionStatus.Ready;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed()
    {
        Status = SessionStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
