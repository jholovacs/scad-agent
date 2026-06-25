using FluentAssertions;
using ScadAgent.Application.Utilities;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Tests;

public class ScadUnitsTests
{
    private const string InchModel = """
        // Hollow cylinder parameters
        inner_diameter = 1.76; // inches
        thickness = 3/16;      // inches
        cylinder(h = 1, r = 1);
        """;

    [Fact]
    public void Parse_detects_explicit_mm_directive()
    {
        ScadUnits.Parse("// @units mm\ncube(10);").Should().Be(LinearUnits.Millimeters);
    }

    [Fact]
    public void Parse_detects_explicit_inch_directive()
    {
        ScadUnits.Parse("// @units in\ncube(1);").Should().Be(LinearUnits.Inches);
    }

    [Fact]
    public void Parse_detects_inch_comments_when_no_directive()
    {
        ScadUnits.Parse(InchModel).Should().Be(LinearUnits.Inches);
    }

    [Fact]
    public void ForRender_wraps_inch_models_with_scale()
    {
        var renderScad = ScadUnits.ForRender(InchModel, LinearUnits.Inches);

        renderScad.Should().Contain("scale(25.4)");
        renderScad.Should().Contain("// @units mm");
        renderScad.Should().Contain("inner_diameter = 1.76");
    }

    [Fact]
    public void ForScadDownload_documents_units_in_banner()
    {
        var download = ScadUnits.ForScadDownload(InchModel, LinearUnits.Inches);

        download.Should().Contain("source dimensions are inches");
        download.Should().Contain("// @units in");
    }
}
