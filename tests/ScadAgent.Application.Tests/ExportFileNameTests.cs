using FluentAssertions;
using ScadAgent.Application.Utilities;

namespace ScadAgent.Application.Tests;

public class ExportFileNameTests
{
    [Fact]
    public void Sanitize_replaces_invalid_characters()
    {
        var result = ExportFileName.Sanitize("My Design: v2/test");
        result.Should().NotContainAny(Path.GetInvalidFileNameChars().Select(c => c.ToString()).ToArray());
        result.Should().Contain("My Design");
    }

    [Fact]
    public void ForIteration_uses_title_and_version()
    {
        ExportFileName.ForIteration("Phone Stand", 2, "stl").Should().Be("Phone Stand-v2.stl");
        ExportFileName.ForIteration("Phone Stand", 2, "stl", includeMillimeterSuffix: true)
            .Should().Be("Phone Stand-v2-mm.stl");
    }

    [Fact]
    public void Sanitize_defaults_when_empty()
    {
        ExportFileName.Sanitize("   ").Should().Be("design");
    }
}
