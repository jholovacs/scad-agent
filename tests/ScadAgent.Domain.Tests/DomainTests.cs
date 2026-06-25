using FluentAssertions;
using ScadAgent.Domain.Entities;
using ScadAgent.Domain.Enums;
using ScadAgent.Domain.ValueObjects;

namespace ScadAgent.Domain.Tests;

public class DesignSessionTests
{
    [Fact]
    public void BeginIteration_allows_refinement_after_ready()
    {
        var session = new DesignSession { Status = SessionStatus.Ready };
        session.BeginIteration();
        session.Status.Should().Be(SessionStatus.Iterating);
    }

    [Fact]
    public void BeginIteration_sets_status_to_iterating()
    {
        var session = new DesignSession { Status = SessionStatus.Draft };
        session.BeginIteration();
        session.Status.Should().Be(SessionStatus.Iterating);
    }

    [Fact]
    public void MarkReady_sets_current_iteration_and_status()
    {
        var session = new DesignSession { Status = SessionStatus.Iterating };
        var iterationId = Guid.NewGuid();

        session.MarkReady(iterationId);

        session.Status.Should().Be(SessionStatus.Ready);
        session.CurrentIterationId.Should().Be(iterationId);
    }
}

public class DesignIterationTests
{
    [Fact]
    public void MarkSucceeded_requires_stl_path()
    {
        var iteration = new DesignIteration();
        var act = () => iteration.MarkSucceeded("", null, null);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkSucceeded_updates_status_and_paths()
    {
        var iteration = new DesignIteration();
        iteration.MarkSucceeded("/tmp/model.stl", "/tmp/preview.png", "done");

        iteration.Status.Should().Be(IterationStatus.Succeeded);
        iteration.StlArtifactPath.Should().Be("/tmp/model.stl");
        iteration.PreviewArtifactPath.Should().Be("/tmp/preview.png");
        iteration.Summary.Should().Be("done");
    }
}

public class ScadSourceTests
{
    [Fact]
    public void ComputeHash_is_stable_for_same_content()
    {
        var a = new ScadSource("cube(10);");
        var b = new ScadSource("cube(10);");
        a.Hash.Should().Be(b.Hash);
    }

    [Fact]
    public void Constructor_rejects_empty_content()
    {
        var act = () => new ScadSource("  ");
        act.Should().Throw<ArgumentException>();
    }
}

public class RenderResultTests
{
    [Fact]
    public void ParseIssues_splits_stderr_lines()
    {
        var issues = RenderResult.ParseIssues("ERROR: line 1\nERROR: line 2");
        issues.Should().HaveCount(2);
    }
}
