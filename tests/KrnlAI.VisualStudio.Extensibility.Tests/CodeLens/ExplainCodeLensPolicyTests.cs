using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.CodeLens;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.CodeLens;

public sealed class ExplainCodeLensPolicyTests
{
    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("method")]
    public void ShouldProvide_SupportedKinds_ShouldReturnTrue(string kind)
    {
        ExplainCodeLensPolicy.ShouldProvide(kind).Should().BeTrue();
    }

    [Theory]
    [InlineData("field")]
    [InlineData("property")]
    [InlineData("event")]
    [InlineData("interface")]
    [InlineData("enum")]
    [InlineData("namespace")]
    [InlineData("")]
    public void ShouldProvide_UnsupportedKinds_ShouldReturnFalse(string kind)
    {
        ExplainCodeLensPolicy.ShouldProvide(kind).Should().BeFalse();
    }

    [Fact]
    public void ShouldProvide_NullKind_ShouldReturnFalse()
    {
        ExplainCodeLensPolicy.ShouldProvide(null).Should().BeFalse();
    }

    [Fact]
    public void Label_ShouldDescribeExplainAction()
    {
        ExplainCodeLensPolicy.Label.Should().Contain("Explain");
    }
}