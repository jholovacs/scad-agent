using ScadAgent.Domain.Enums;

namespace ScadAgent.Domain.Entities;

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid? IterationId { get; set; }
    public MessageRole Role { get; set; }
    public MessageIntent? Intent { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public DesignSession? Session { get; set; }
    public DesignIteration? Iteration { get; set; }
}
