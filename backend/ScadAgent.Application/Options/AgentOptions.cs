namespace ScadAgent.Application.Options;

public class AgentOptions
{
    public const string SectionName = "Agent";

    public int MaxCorrectionRetries { get; set; } = 3;
    public int MaxContextMessages { get; set; } = 20;
    public int OpenScadTimeoutSeconds { get; set; } = 60;
    public string OpenScadExecutablePath { get; set; } = "openscad";
    public string? OpenScadRemoteUrl { get; set; }
}
