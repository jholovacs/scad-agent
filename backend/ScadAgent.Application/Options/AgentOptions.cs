namespace ScadAgent.Application.Options;

public class AgentOptions
{
    public const string SectionName = "Agent";

    public int MaxCorrectionRetries { get; set; } = 3;
    public int ContextCompressionThresholdChars { get; set; } = 32_000;
    public int ContextKeepRecentMessages { get; set; } = 6;
    public int OpenScadTimeoutSeconds { get; set; } = 60;
    public string OpenScadExecutablePath { get; set; } = "openscad";
    public string? OpenScadRemoteUrl { get; set; }
}
