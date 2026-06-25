using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        var service = new SessionService(db);

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
        var service = new SessionService(db);
        var session = await service.CreateSessionAsync("msg test");

        await service.AddUserMessageAsync(session.Id, "Build a sphere");

        var messages = await db.Messages.Where(m => m.SessionId == session.Id).ToListAsync();
        messages.Should().ContainSingle(m => m.Role == MessageRole.User && m.Content == "Build a sphere");
    }

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
