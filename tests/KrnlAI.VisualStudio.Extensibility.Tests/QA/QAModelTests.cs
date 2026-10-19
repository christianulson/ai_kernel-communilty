using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.QA;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.QA;

public sealed class QAModelTests
{
    [Fact]
    public void AddCase_ShouldAppendAndReturnIndex()
    {
        var model = new QAModel();

        var index = model.AddCase("login flow");

        index.Should().Be(0);
        model.Cases.Should().ContainSingle().Which.Name.Should().Be("login flow");
    }
}