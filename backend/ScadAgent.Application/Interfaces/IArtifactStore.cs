namespace ScadAgent.Application.Interfaces;

public interface IArtifactStore
{
    string GetIterationDirectory(Guid sessionId, Guid iterationId);
    Task<string> SaveScadAsync(Guid sessionId, Guid iterationId, string content, CancellationToken cancellationToken = default);
    Task<Stream?> OpenStlAsync(string? artifactPath, CancellationToken cancellationToken = default);
    Task<Stream?> OpenPreviewAsync(string? artifactPath, CancellationToken cancellationToken = default);
}
