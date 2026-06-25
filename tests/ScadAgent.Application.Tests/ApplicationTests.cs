using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ScadAgent.Application.Diagnostics;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Application.Services;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;
using ScadAgent.Domain.ValueObjects;

namespace ScadAgent.Application.Tests;

public class ScadCodeExtractorTests
{
    [Fact]
    public void Extract_parses_scad_fence()
    {
        const string response = """
            Here is the model:
            ```scad
            cube(10);
            ```
            """;

        var code = ScadCodeExtractor.Extract(response);
        code.Should().Be("cube(10);");
    }

    [Fact]
    public void Extract_throws_when_no_code_found()
    {
        var act = () => ScadCodeExtractor.Extract("no code here");
        act.Should().Throw<InvalidOperationException>();
    }
}

public class ContextManagerTests
{
    [Fact]
    public void BuildMessages_includes_system_user_and_scad_context()
    {
        var manager = new ContextManager();
        var messages = manager.BuildMessages(new DesignContext(
            "cube(5);",
            "Make it bigger",
            null,
            [new ConversationMessageContext("user", "hello")],
            "parse error"));

        messages.Should().Contain(m => m.Role == "system");
        messages.Should().Contain(m => m.Content.Contains("cube(5);"));
        messages.Should().Contain(m => m.Content.Contains("parse error"));
        messages.Last().Content.Should().Be("Make it bigger");
    }

    [Fact]
    public void BuildMessages_includes_session_memory_when_present()
    {
        var manager = new ContextManager();
        var messages = manager.BuildMessages(new DesignContext(
            null,
            "Continue",
            "User wants a hollow cylinder.",
            [],
            null));

        messages.Should().Contain(m => m.Content.Contains("Session memory"));
        messages.Should().Contain(m => m.Content.Contains("hollow cylinder"));
    }
}

public class DesignAgentServiceTests
{
    [Fact]
    public async Task RunIterationAsync_succeeds_when_render_passes()
    {
        var sessionId = Guid.NewGuid();
        var session = new DesignSession
        {
            Id = sessionId,
            Status = SessionStatus.Draft,
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
        sessions.AddIterationAsync(Arg.Any<DesignIteration>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<DesignIteration>());

        var ollama = Substitute.For<IOllamaService>();
        ollama.ChatAsync(Arg.Any<IReadOnlyList<OllamaMessage>>(), Arg.Any<CancellationToken>())
            .Returns("```scad\ncube(10);\n```");

        var openScad = Substitute.For<IOpenScadService>();
        openScad.RenderAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RenderResult.Succeeded("/tmp/model.stl", "/tmp/preview.png", TimeSpan.FromSeconds(1)));

        var artifacts = Substitute.For<IArtifactStore>();
        artifacts.GetIterationDirectory(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns("/tmp/out");

        var notifier = Substitute.For<IAgentNotifier>();
        var contextManager = new ContextManager();
        var contextPreparer = Substitute.For<IConversationContextPreparer>();
        contextPreparer.PrepareAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new PreparedConversationHistory(
                null,
                [new ConversationMessageContext("user", "Create a cube")]));

        var service = new DesignAgentService(
            sessions,
            contextManager,
            contextPreparer,
            ollama,
            openScad,
            artifacts,
            notifier,
            Microsoft.Extensions.Options.Options.Create(new AgentOptions { MaxCorrectionRetries = 1 }),
            Microsoft.Extensions.Options.Options.Create(new OllamaOptions { Model = "test", BaseUrl = "http://localhost:11434" }),
            NullLogger<DesignAgentService>.Instance);

        var iterationId = await service.RunIterationAsync(sessionId);

        iterationId.Should().NotBeEmpty();
        await notifier.Received().NotifyIterationCompletedAsync(Arg.Any<Application.DTOs.AgentProgressDto>(), Arg.Any<CancellationToken>());
        await sessions.Received().AddMessageAsync(sessionId, MessageRole.Assistant, Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<MessageIntent?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunIterationAsync_records_diagnostic_when_ollama_fails()
    {
        var sessionId = Guid.NewGuid();
        var session = new DesignSession
        {
            Id = sessionId,
            Status = SessionStatus.Draft,
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
        sessions.AddIterationAsync(Arg.Any<DesignIteration>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<DesignIteration>());

        var ollama = Substitute.For<IOllamaService>();
        ollama.ChatAsync(Arg.Any<IReadOnlyList<OllamaMessage>>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new OllamaRequestException(
                HttpStatusCode.BadGateway,
                """{"error":"model not found"}""",
                "missing-model",
                "http://ollama.local:11434",
                "Ollama returned HTTP 502"));

        var contextPreparer = Substitute.For<IConversationContextPreparer>();
        contextPreparer.PrepareAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new PreparedConversationHistory(
                null,
                [new ConversationMessageContext("user", "Create a cube")]));

        var service = new DesignAgentService(
            sessions,
            contextManager: new ContextManager(),
            contextPreparer,
            ollama,
            Substitute.For<IOpenScadService>(),
            Substitute.For<IArtifactStore>(),
            Substitute.For<IAgentNotifier>(),
            Microsoft.Extensions.Options.Options.Create(new AgentOptions()),
            Microsoft.Extensions.Options.Options.Create(new OllamaOptions { Model = "missing-model", BaseUrl = "http://ollama.local:11434" }),
            NullLogger<DesignAgentService>.Instance);

        var iterationId = await service.RunIterationAsync(sessionId);

        iterationId.Should().NotBeEmpty();
        await sessions.Received().AddMessageAsync(
            sessionId,
            MessageRole.Assistant,
            Arg.Is<string>(s => s.Contains("=== SCAD Agent Diagnostic Report ===") && s.Contains("model not found")),
            Arg.Any<Guid?>(),
            Arg.Any<MessageIntent?>(),
            Arg.Any<CancellationToken>());
        await sessions.Received().UpdateIterationAsync(
            Arg.Is<DesignIteration>(i => i.DiagnosticLog != null && i.Status == IterationStatus.Failed),
            Arg.Any<CancellationToken>());
    }
}

public class AgentDiagnosticReportTests
{
    [Fact]
    public void Format_includes_ollama_response_body()
    {
        var ex = new OllamaRequestException(HttpStatusCode.BadGateway, """{"error":"timeout"}""", "m", "http://x", "failed");
        var report = AgentDiagnosticReport.FormatOllamaFailure(
            "test",
            Guid.NewGuid(),
            Guid.NewGuid(),
            new OllamaOptionsSnapshot("http://x", "m"),
            ex);

        report.Should().Contain("=== SCAD Agent Diagnostic Report ===");
        report.Should().Contain("timeout");
    }
}
