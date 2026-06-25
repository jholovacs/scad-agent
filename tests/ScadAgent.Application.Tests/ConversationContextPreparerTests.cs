using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Application.Services;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Tests;

public class ConversationContextPreparerTests
{
    [Fact]
    public async Task PrepareAsync_returns_all_messages_when_under_threshold()
    {
        var sessionId = Guid.NewGuid();
        var session = new DesignSession
        {
            Id = sessionId,
            Messages =
            [
                new ConversationMessage
                {
                    Id = Guid.NewGuid(),
                    Role = MessageRole.User,
                    Content = "Create a cube",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var sessions = Substitute.For<ISessionService>();
        sessions.GetSessionEntityAsync(sessionId, Arg.Any<CancellationToken>()).Returns(session);

        var ollama = Substitute.For<IOllamaService>();
        var preparer = new ConversationContextPreparer(
            sessions,
            ollama,
            Microsoft.Extensions.Options.Options.Create(new AgentOptions { ContextCompressionThresholdChars = 10_000 }),
            NullLogger<ConversationContextPreparer>.Instance);

        var prepared = await preparer.PrepareAsync(sessionId);

        prepared.Summary.Should().BeNull();
        prepared.Messages.Should().HaveCount(1);
        await ollama.DidNotReceive().ChatAsync(Arg.Any<IReadOnlyList<OllamaMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PrepareAsync_compresses_old_messages_when_over_threshold()
    {
        var sessionId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var session = new DesignSession
        {
            Id = sessionId,
            Messages =
            [
                new ConversationMessage
                {
                    Id = firstId,
                    Role = MessageRole.User,
                    Content = new string('a', 20_000),
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
                },
                new ConversationMessage
                {
                    Id = secondId,
                    Role = MessageRole.Assistant,
                    Content = "done",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
                },
                new ConversationMessage
                {
                    Id = Guid.NewGuid(),
                    Role = MessageRole.User,
                    Content = "latest",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        var sessions = Substitute.For<ISessionService>();
        sessions.GetSessionEntityAsync(sessionId, Arg.Any<CancellationToken>()).Returns(session);

        var ollama = Substitute.For<IOllamaService>();
        ollama.ChatAsync(Arg.Any<IReadOnlyList<OllamaMessage>>(), Arg.Any<CancellationToken>())
            .Returns("Compressed memory");

        var preparer = new ConversationContextPreparer(
            sessions,
            ollama,
            Microsoft.Extensions.Options.Options.Create(new AgentOptions
            {
                ContextCompressionThresholdChars = 1_000,
                ContextKeepRecentMessages = 1
            }),
            NullLogger<ConversationContextPreparer>.Instance);

        var prepared = await preparer.PrepareAsync(sessionId);

        prepared.Summary.Should().Be("Compressed memory");
        prepared.Messages.Should().ContainSingle(m => m.Content == "latest");
        await sessions.Received().UpdateSessionAsync(
            Arg.Is<DesignSession>(s =>
                s.ContextSummary == "Compressed memory"
                && s.ContextSummarizedThroughMessageId == secondId),
            Arg.Any<CancellationToken>());
    }
}
