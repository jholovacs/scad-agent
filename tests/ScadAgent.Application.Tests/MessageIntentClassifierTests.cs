using FluentAssertions;
using NSubstitute;
using ScadAgent.Application.Interfaces;
using ScadAgent.Application.Services;
using ScadAgent.Domain.Enums;

namespace ScadAgent.Application.Tests;

public class MessageIntentClassifierTests
{
    [Theory]
    [InlineData("QUESTION", MessageIntent.Ask)]
    [InlineData("This is a QUESTION about dimensions", MessageIntent.Ask)]
    [InlineData("DESIGN", MessageIntent.Design)]
    [InlineData("```scad\ncube(1);\n```", MessageIntent.Design)]
    public async Task ClassifyAsync_maps_model_response_to_intent(string response, MessageIntent expected)
    {
        var ollama = Substitute.For<IOllamaService>();
        ollama.ChatAsync(Arg.Any<IReadOnlyList<OllamaMessage>>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var classifier = new MessageIntentClassifier(ollama);

        var intent = await classifier.ClassifyAsync("user text", hasExistingDesign: true);

        intent.Should().Be(expected);
    }
}
