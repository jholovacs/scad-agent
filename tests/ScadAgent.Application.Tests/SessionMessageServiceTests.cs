using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Services;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Tests;

public class SessionMessageServiceTests
{
    [Fact]
    public async Task HandleAsync_routes_questions_to_conversation()
    {
        var sessionId = Guid.NewGuid();
        var sessions = Substitute.For<ISessionService>();
        sessions.GetSessionEntityAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new DesignSession
            {
                Id = sessionId,
                Iterations =
                [
                    new DesignIteration
                    {
                        Id = Guid.NewGuid(),
                        Status = IterationStatus.Succeeded,
                        Version = 1,
                        ScadContent = "cube(1);",
                        ScadHash = "abc"
                    }
                ]
            });

        var classifier = Substitute.For<IMessageIntentClassifier>();
        classifier.ClassifyAsync("what are the dimensions?", true, Arg.Any<CancellationToken>())
            .Returns(MessageIntent.Ask);

        var conversation = Substitute.For<IConversationService>();
        var agent = Substitute.For<IDesignAgentService>();

        var service = new SessionMessageService(
            sessions,
            classifier,
            conversation,
            agent,
            NullLogger<SessionMessageService>.Instance);

        await service.HandleAsync(sessionId, "what are the dimensions?");

        await sessions.Received().AddUserMessageAsync(
            sessionId,
            "what are the dimensions?",
            MessageIntent.Ask,
            Arg.Any<CancellationToken>());
        await conversation.Received().ReplyAsync(sessionId, "what are the dimensions?", Arg.Any<CancellationToken>());
        await agent.DidNotReceive().RunIterationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_routes_design_instructions_to_agent()
    {
        var sessionId = Guid.NewGuid();
        var sessions = Substitute.For<ISessionService>();
        sessions.GetSessionEntityAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new DesignSession { Id = sessionId });

        var classifier = Substitute.For<IMessageIntentClassifier>();
        classifier.ClassifyAsync("make it taller", false, Arg.Any<CancellationToken>())
            .Returns(MessageIntent.Design);

        var conversation = Substitute.For<IConversationService>();
        var agent = Substitute.For<IDesignAgentService>();

        var service = new SessionMessageService(
            sessions,
            classifier,
            conversation,
            agent,
            NullLogger<SessionMessageService>.Instance);

        await service.HandleAsync(sessionId, "make it taller");

        await sessions.Received().AddUserMessageAsync(
            sessionId,
            "make it taller",
            MessageIntent.Design,
            Arg.Any<CancellationToken>());
        await agent.Received().RunIterationAsync(sessionId, Arg.Any<CancellationToken>());
        await conversation.DidNotReceive().ReplyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
