namespace ScadAgent.Application.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string DatabasePath { get; set; } = "data/scad-agent.db";
    public string ArtifactsPath { get; set; } = "data/artifacts";
}
