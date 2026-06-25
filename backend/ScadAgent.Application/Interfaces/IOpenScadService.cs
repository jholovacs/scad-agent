using ScadAgent.Domain.ValueObjects;

namespace ScadAgent.Application.Interfaces;

public interface IOpenScadService
{
    Task<RenderResult> RenderAsync(string scadContent, string outputDirectory, CancellationToken cancellationToken = default);
    bool IsAvailable();
}
