using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScadAgent.Application.Diagnostics;
using ScadAgent.Application.DTOs;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Application.Services;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;
using ScadAgent.Domain.ValueObjects;

namespace ScadAgent.Application.Services;

public class DesignAgentService : IDesignAgentService
{
    private readonly ISessionService _sessions;
    private readonly IContextManager _contextManager;
    private readonly IOllamaService _ollama;
    private readonly IOpenScadService _openScad;
    private readonly IArtifactStore _artifacts;
    private readonly IAgentNotifier _notifier;
    private readonly AgentOptions _agentOptions;
    private readonly OllamaOptions _ollamaOptions;
    private readonly ILogger<DesignAgentService> _logger;

    public DesignAgentService(
        ISessionService sessions,
        IContextManager contextManager,
        IOllamaService ollama,
        IOpenScadService openScad,
        IArtifactStore artifacts,
        IAgentNotifier notifier,
        IOptions<AgentOptions> agentOptions,
        IOptions<OllamaOptions> ollamaOptions,
        ILogger<DesignAgentService> logger)
    {
        _sessions = sessions;
        _contextManager = contextManager;
        _ollama = ollama;
        _openScad = openScad;
        _artifacts = artifacts;
        _notifier = notifier;
        _agentOptions = agentOptions.Value;
        _ollamaOptions = ollamaOptions.Value;
        _logger = logger;
    }

    public async Task<Guid> RunIterationAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetSessionEntityAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Session {sessionId} not found.");

        var latestUserMessage = session.Messages
            .Where(m => m.Role == MessageRole.User)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No user message to process.");

        session.BeginIteration();
        await _sessions.UpdateSessionAsync(session, cancellationToken);

        var currentScad = session.Iterations
            .Where(i => i.Status == IterationStatus.Succeeded)
            .OrderByDescending(i => i.Version)
            .FirstOrDefault()?.ScadContent;

        var version = session.Iterations.Count == 0 ? 1 : session.Iterations.Max(i => i.Version) + 1;
        var iteration = new DesignIteration
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Version = version,
            Status = IterationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _sessions.AddIterationAsync(iteration, cancellationToken);

        await _notifier.NotifyIterationStartedAsync(
            new AgentProgressDto(sessionId, iteration.Id, "started", "Generating OpenSCAD design"),
            cancellationToken);

        var lastError = session.Iterations
            .Where(i => i.Status == IterationStatus.Failed)
            .OrderByDescending(i => i.Version)
            .FirstOrDefault()?.RenderError;

        var context = new DesignContext(
            currentScad,
            latestUserMessage.Content,
            session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ConversationMessageContext(MapRole(m.Role), m.Content))
                .ToList(),
            lastError);

        var messages = _contextManager.BuildMessages(context);
        string scadContent;
        string assistantText;

        await _notifier.NotifyIterationProgressAsync(
            new AgentProgressDto(sessionId, iteration.Id, "llm", "Requesting design from Ollama"),
            cancellationToken);

        try
        {
            assistantText = await _ollama.ChatAsync(messages, cancellationToken);
            scadContent = ScadCodeExtractor.Extract(assistantText);
        }
        catch (Exception ex)
        {
            return await FailIterationAsync(session, iteration, "Ollama initial generation", ex, cancellationToken);
        }

        var scadSource = new ScadSource(scadContent);
        iteration.ScadContent = scadSource.Content;
        iteration.ScadHash = scadSource.Hash;
        iteration.AssistantSummary = assistantText.Length > 500 ? assistantText[..500] : assistantText;
        await _artifacts.SaveScadAsync(sessionId, iteration.Id, scadContent, cancellationToken);

        var attempts = 0;
        RenderResult? renderResult = null;

        while (attempts <= _agentOptions.MaxCorrectionRetries)
        {
            iteration.MarkRendering();
            iteration.CorrectionAttempts = attempts;
            await _sessions.UpdateIterationAsync(iteration, cancellationToken);

            await _notifier.NotifyIterationProgressAsync(
                new AgentProgressDto(sessionId, iteration.Id, "render", $"Rendering attempt {attempts + 1}"),
                cancellationToken);

            var outputDir = _artifacts.GetIterationDirectory(sessionId, iteration.Id);
            renderResult = await _openScad.RenderAsync(scadContent, outputDir, cancellationToken);

            if (renderResult.Success)
                break;

            attempts++;
            if (attempts > _agentOptions.MaxCorrectionRetries)
                break;

            var correctionMessages = messages.ToList();
            correctionMessages.Add(new OllamaMessage("assistant", assistantText));
            correctionMessages.Add(new OllamaMessage(
                "user",
                $"Render failed:\n{renderResult.StdErr}\nProvide corrected OpenSCAD in a ```scad block."));

            await _notifier.NotifyIterationProgressAsync(
                new AgentProgressDto(sessionId, iteration.Id, "correction", $"Correcting errors (attempt {attempts})"),
                cancellationToken);

            try
            {
                assistantText = await _ollama.ChatAsync(correctionMessages, cancellationToken);
                scadContent = ScadCodeExtractor.Extract(assistantText);
            }
            catch (Exception ex)
            {
                return await FailIterationAsync(
                    session,
                    iteration,
                    $"Ollama correction attempt {attempts}",
                    ex,
                    cancellationToken,
                    renderResult.StdErr,
                    assistantText);
            }

            scadSource = new ScadSource(scadContent);
            iteration.ScadContent = scadSource.Content;
            iteration.ScadHash = scadSource.Hash;
            await _artifacts.SaveScadAsync(sessionId, iteration.Id, scadContent, cancellationToken);
        }

        if (renderResult is null || !renderResult.Success)
        {
            var error = renderResult?.StdErr ?? "Render failed without error output.";
            var diagnostic = AgentDiagnosticReport.Format(
                "OpenSCAD render",
                sessionId,
                iteration.Id,
                _ollamaOptions.Model,
                _ollamaOptions.BaseUrl,
                new InvalidOperationException(error),
                llmResponsePreview: assistantText);

            iteration.DiagnosticLog = diagnostic;
            iteration.MarkFailed(error);
            await _sessions.UpdateIterationAsync(iteration, cancellationToken);
            session.MarkFailed();
            await _sessions.UpdateSessionAsync(session, cancellationToken);

            await _sessions.AddMessageAsync(sessionId, MessageRole.Assistant, diagnostic, cancellationToken);

            await _notifier.NotifyIterationFailedAsync(
                new AgentProgressDto(sessionId, iteration.Id, "failed", error, diagnostic),
                cancellationToken);
            return iteration.Id;
        }

        iteration.MarkSucceeded(renderResult.StlPath!, renderResult.PreviewPath, iteration.AssistantSummary);
        iteration.DiagnosticLog = null;
        await _sessions.UpdateIterationAsync(iteration, cancellationToken);
        session.MarkReady(iteration.Id);
        await _sessions.UpdateSessionAsync(session, cancellationToken);

        await _sessions.AddMessageAsync(
            sessionId,
            MessageRole.Assistant,
            iteration.AssistantSummary ?? "Design rendered successfully.",
            cancellationToken);

        await _notifier.NotifyIterationCompletedAsync(
            new AgentProgressDto(sessionId, iteration.Id, "completed", "Design rendered successfully"),
            cancellationToken);

        return iteration.Id;
    }

    private async Task<Guid> FailIterationAsync(
        DesignSession session,
        DesignIteration iteration,
        string phase,
        Exception exception,
        CancellationToken cancellationToken,
        string? renderError = null,
        string? llmResponsePreview = null)
    {
        _logger.LogError(exception, "Iteration failed during {Phase} for session {SessionId}", phase, session.Id);

        var ollamaSnapshot = new OllamaOptionsSnapshot(_ollamaOptions.BaseUrl, _ollamaOptions.Model);
        var diagnostic = exception is OllamaRequestException ollamaEx
            ? AgentDiagnosticReport.FormatOllamaFailure(phase, session.Id, iteration.Id, ollamaSnapshot, ollamaEx)
            : AgentDiagnosticReport.Format(
                phase,
                session.Id,
                iteration.Id,
                _ollamaOptions.Model,
                _ollamaOptions.BaseUrl,
                exception,
                llmResponsePreview: llmResponsePreview);

        if (!string.IsNullOrWhiteSpace(renderError))
        {
            diagnostic += $"\n--- OpenSCAD stderr ---\n{renderError}";
        }

        var summary = exception.Message;
        iteration.DiagnosticLog = diagnostic;
        iteration.MarkFailed(summary);
        await _sessions.UpdateIterationAsync(iteration, cancellationToken);
        session.MarkFailed();
        await _sessions.UpdateSessionAsync(session, cancellationToken);

        await _sessions.AddMessageAsync(session.Id, MessageRole.Assistant, diagnostic, cancellationToken);

        await _notifier.NotifyIterationFailedAsync(
            new AgentProgressDto(session.Id, iteration.Id, "failed", summary, diagnostic),
            cancellationToken);

        return iteration.Id;
    }

    private static string MapRole(MessageRole role) => role switch
    {
        MessageRole.User => "user",
        MessageRole.Assistant => "assistant",
        _ => "system"
    };
}
