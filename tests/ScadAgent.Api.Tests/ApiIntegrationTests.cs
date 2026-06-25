using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ScadAgent.Application.DTOs;
using ScadAgent.Application.Interfaces;
using ScadAgent.Infrastructure.Persistence;

namespace ScadAgent.Api.Tests;

public class ScadAgentWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ScadAgentDbContext>>();
            services.RemoveAll<ScadAgentDbContext>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<ScadAgentDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IDesignAgentService>();
            services.AddScoped<IDesignAgentService>(_ => Substitute.For<IDesignAgentService>());

            services.RemoveAll<IOllamaService>();
            var ollama = Substitute.For<IOllamaService>();
            ollama.IsReachableAsync(Arg.Any<CancellationToken>()).Returns(true);
            services.AddSingleton(ollama);

            services.RemoveAll<IOpenScadService>();
            var openScad = Substitute.For<IOpenScadService>();
            openScad.IsAvailable().Returns(true);
            services.AddSingleton(openScad);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _connection?.Dispose();
        base.Dispose(disposing);
    }
}

public class ApiIntegrationTests : IClassFixture<ScadAgentWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static bool _databaseInitialized;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiIntegrationTests(ScadAgentWebApplicationFactory factory)
    {
        if (!_databaseInitialized)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ScadAgentDbContext>();
            db.Database.EnsureCreated();
            _databaseInitialized = true;
        }

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<HealthDto>(JsonOptions);
        payload!.ApiHealthy.Should().BeTrue();
        payload.OllamaReachable.Should().BeTrue();
    }

    [Fact]
    public async Task Sessions_crud_flow_works()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest("Gear"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<SessionDetailDto>(JsonOptions);

        var getResponse = await _client.GetAsync($"/api/sessions/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await _client.GetAsync("/api/sessions");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<List<SessionSummaryDto>>(JsonOptions);
        list!.Should().Contain(s => s.Id == created.Id);
    }
}
