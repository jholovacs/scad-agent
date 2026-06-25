using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;
using ScadAgent.Application.Services;
using ScadAgent.Infrastructure.Ollama;
using ScadAgent.Infrastructure.OpenScad;
using ScadAgent.Infrastructure.Persistence;
using ScadAgent.Infrastructure.Storage;

namespace ScadAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        var storage = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        var dbDirectory = Path.GetDirectoryName(Path.GetFullPath(storage.DatabasePath));
        if (!string.IsNullOrEmpty(dbDirectory))
            Directory.CreateDirectory(dbDirectory);

        services.AddDbContext<ScadAgentDbContext>(options =>
            options.UseSqlite($"Data Source={storage.DatabasePath}"));

        services.AddHttpClient<IOllamaService, OllamaHttpClient>();

        var agent = configuration.GetSection(AgentOptions.SectionName).Get<AgentOptions>() ?? new AgentOptions();
        if (!string.IsNullOrWhiteSpace(agent.OpenScadRemoteUrl))
        {
            services.AddHttpClient<IOpenScadService, OpenScadRemoteClient>()
                .ConfigureHttpClient(client =>
                    client.Timeout = TimeSpan.FromSeconds(agent.OpenScadTimeoutSeconds + 15));
        }
        else
            services.AddSingleton<IOpenScadService, OpenScadProcessRunner>();

        services.AddScoped<ISessionService, SessionService>();
        services.AddSingleton<IArtifactStore, LocalArtifactStore>();

        services.AddScoped<IContextManager, ContextManager>();
        services.AddScoped<IConversationContextPreparer, ConversationContextPreparer>();
        services.AddScoped<IMessageIntentClassifier, MessageIntentClassifier>();
        services.AddScoped<ISessionMessageService, SessionMessageService>();
        services.AddScoped<IDesignAgentService, DesignAgentService>();
        services.AddScoped<IConversationService, ConversationService>();

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScadAgentDbContext>();
        await db.Database.MigrateAsync();
    }
}
