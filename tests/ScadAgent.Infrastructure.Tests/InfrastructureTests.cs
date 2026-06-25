using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;
using ScadAgent.Infrastructure.Persistence;
using ScadAgent.Infrastructure.Storage;

namespace ScadAgent.Infrastructure.Tests;

public class SessionServiceTests
{
    [Fact]
    public async Task CreateSessionAsync_persists_session()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var created = await service.CreateSessionAsync("Test box");

        created.Title.Should().Be("Test box");
        created.Status.Should().Be(SessionStatus.Draft);

        var stored = await db.Sessions.CountAsync();
        stored.Should().Be(1);
    }

    [Fact]
    public async Task AddMessageAsync_persists_user_message()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var session = await service.CreateSessionAsync("msg test");

        await service.AddUserMessageAsync(session.Id, "Build a sphere");

        var messages = await db.Messages.Where(m => m.SessionId == session.Id).ToListAsync();
        messages.Should().ContainSingle(m => m.Role == MessageRole.User && m.Content == "Build a sphere");
    }

    [Fact]
    public async Task GetMessagesAsync_returns_messages_ordered_newest_first()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var session = await service.CreateSessionAsync("chat test");
        var older = DateTimeOffset.UtcNow.AddMinutes(-5);
        var newer = DateTimeOffset.UtcNow;

        db.Messages.AddRange(
            new ConversationMessage
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                Role = MessageRole.User,
                Content = "older",
                CreatedAt = older
            },
            new ConversationMessage
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                Role = MessageRole.Assistant,
                Content = "newer",
                CreatedAt = newer
            });
        await db.SaveChangesAsync();

        var page = await service.GetMessagesAsync(session.Id, limit: 10, before: null);

        page.Messages.Should().HaveCount(2);
        page.Messages[0].Content.Should().Be("newer");
        page.Messages[1].Content.Should().Be("older");
    }

    [Fact]
    public async Task GetIterationsPageAsync_returns_newest_versions_first()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var session = await service.CreateSessionAsync("versions test");

        for (var version = 1; version <= 3; version++)
        {
            await service.AddIterationAsync(new DesignIteration
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                Version = version,
                Status = IterationStatus.Succeeded,
                ScadContent = $"cube({version});",
                ScadHash = $"hash-{version}",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(version)
            });
        }

        var page = await service.GetIterationsPageAsync(session.Id, limit: 2);

        page.Iterations.Should().HaveCount(2);
        page.HasMore.Should().BeTrue();
        page.OldestVersion.Should().Be(2);
        page.Iterations[0].Version.Should().Be(3);
        page.Iterations[1].Version.Should().Be(2);

        var olderPage = await service.GetIterationsPageAsync(session.Id, limit: 2, beforeVersion: page.OldestVersion);
        olderPage.Iterations.Should().ContainSingle(i => i.Version == 1);
        olderPage.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSessionAsync_removes_session_messages_and_iterations()
    {
        await using var db = CreateDb();
        var artifacts = new RecordingArtifactStore();
        var service = CreateService(db, artifacts);
        var session = await service.CreateSessionAsync("delete me");
        await service.AddUserMessageAsync(session.Id, "hello");

        var iteration = new DesignIteration
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Version = 1,
            Status = IterationStatus.Succeeded,
            ScadContent = "cube(1);",
            ScadHash = "abc",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await service.AddIterationAsync(iteration);

        var deleted = await service.DeleteSessionAsync(session.Id);

        deleted.Should().BeTrue();
        db.ChangeTracker.Clear();
        (await db.Sessions.FindAsync(session.Id)).Should().BeNull();
        (await db.Messages.CountAsync()).Should().Be(0);
        (await db.Iterations.CountAsync()).Should().Be(0);
        artifacts.DeletedSessionId.Should().Be(session.Id);
    }

    private sealed class RecordingArtifactStore : IArtifactStore
    {
        public Guid? DeletedSessionId { get; private set; }

        public string GetIterationDirectory(Guid sessionId, Guid iterationId) =>
            Path.Combine(Path.GetTempPath(), sessionId.ToString("N"), iterationId.ToString("N"));

        public Task<string> SaveScadAsync(Guid sessionId, Guid iterationId, string content, CancellationToken cancellationToken = default) =>
            Task.FromResult(Path.Combine(GetIterationDirectory(sessionId, iterationId), "model.scad"));

        public Task<Stream?> OpenStlAsync(string? artifactPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public Task<Stream?> OpenPreviewAsync(string? artifactPath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(null);

        public void DeleteSessionArtifacts(Guid sessionId) => DeletedSessionId = sessionId;
    }

    private static SessionService CreateService(ScadAgentDbContext db, IArtifactStore? artifacts = null) =>
        new(db, artifacts ?? new RecordingArtifactStore());

    private static ScadAgentDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ScadAgentDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var db = new ScadAgentDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }
}

public class LocalArtifactStoreTests
{
    [Fact]
    public async Task SaveScadAsync_writes_file_to_iteration_directory()
    {
        var root = Path.Combine(Path.GetTempPath(), "scad-agent-tests", Guid.NewGuid().ToString("N"));
        var store = new LocalArtifactStore(Options.Create(new StorageOptions { ArtifactsPath = root }));

        var sessionId = Guid.NewGuid();
        var iterationId = Guid.NewGuid();
        var path = await store.SaveScadAsync(sessionId, iterationId, "cube(1);");

        File.Exists(path).Should().BeTrue();
        Directory.Exists(store.GetIterationDirectory(sessionId, iterationId)).Should().BeTrue();

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void DeleteSessionArtifacts_removes_session_directory()
    {
        var root = Path.Combine(Path.GetTempPath(), "scad-agent-tests", Guid.NewGuid().ToString("N"));
        var store = new LocalArtifactStore(Options.Create(new StorageOptions { ArtifactsPath = root }));

        var sessionId = Guid.NewGuid();
        var iterationId = Guid.NewGuid();
        Directory.CreateDirectory(store.GetIterationDirectory(sessionId, iterationId));

        store.DeleteSessionArtifacts(sessionId);

        Directory.Exists(store.GetIterationDirectory(sessionId, iterationId)).Should().BeFalse();
        Directory.Delete(root, recursive: true);
    }
}

public class OpenScadRunnerTests
{
    [Fact]
    [Trait("Category", "OpenScad")]
    public async Task RenderAsync_produces_stl_for_valid_scad()
    {
        var runner = new ScadAgent.Infrastructure.OpenScad.OpenScadProcessRunner(
            Options.Create(new Application.Options.AgentOptions { OpenScadTimeoutSeconds = 30 }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ScadAgent.Infrastructure.OpenScad.OpenScadProcessRunner>.Instance);

        if (!runner.IsAvailable())
            return;

        var outputDir = Path.Combine(Path.GetTempPath(), "scad-agent-openscad", Guid.NewGuid().ToString("N"));
        var result = await runner.RenderAsync("cube(10);", outputDir);

        result.Success.Should().BeTrue();
        File.Exists(result.StlPath!).Should().BeTrue();

        Directory.Delete(outputDir, recursive: true);
    }
}
