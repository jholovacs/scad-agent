using Microsoft.Extensions.Options;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Options;

namespace ScadAgent.Infrastructure.Storage;

public class LocalArtifactStore : IArtifactStore
{
    private readonly StorageOptions _options;

    public LocalArtifactStore(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(_options.ArtifactsPath);
    }

    public string GetIterationDirectory(Guid sessionId, Guid iterationId) =>
        Path.Combine(_options.ArtifactsPath, sessionId.ToString("N"), iterationId.ToString("N"));

    public async Task<string> SaveScadAsync(Guid sessionId, Guid iterationId, string content, CancellationToken cancellationToken = default)
    {
        var directory = GetIterationDirectory(sessionId, iterationId);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "model.scad");
        await File.WriteAllTextAsync(path, content, cancellationToken);
        return path;
    }

    public Task<Stream?> OpenStlAsync(string? artifactPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactPath) || !File.Exists(artifactPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(artifactPath);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<Stream?> OpenPreviewAsync(string? artifactPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactPath) || !File.Exists(artifactPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(artifactPath);
        return Task.FromResult<Stream?>(stream);
    }
}
