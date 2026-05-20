using KrnlAI.VisualStudio.Commands.ChatCommands;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Commands;

public sealed class OutputFormatterTests
{
    [Fact]
    public void AsCodeBlock_WithLanguage_ShouldWrapInFencedBlock()
    {
        var result = OutputFormatter.AsCodeBlock("var x = 1;", "csharp");
        result.Should().Be("```csharp\nvar x = 1;\n```");
    }

    [Fact]
    public void AsCodeBlock_WithoutLanguage_ShouldUseEmptyLang()
    {
        var result = OutputFormatter.AsCodeBlock("hello");
        result.Should().Be("```\nhello\n```");
    }

    [Fact]
    public void AsCodeBlock_WithNullLanguage_ShouldUseEmptyLang()
    {
        var result = OutputFormatter.AsCodeBlock("test", null);
        result.Should().Be("```\ntest\n```");
    }

    [Fact]
    public void AsSuccess_ShouldPrefixWithCheckmark()
    {
        OutputFormatter.AsSuccess("done").Should().Be("✅ done");
    }

    [Fact]
    public void AsError_ShouldPrefixWithCross()
    {
        OutputFormatter.AsError("fail").Should().Be("❌ fail");
    }

    [Fact]
    public void AsWarning_ShouldPrefixWithWarning()
    {
        OutputFormatter.AsWarning("caution").Should().Be("⚠️ caution");
    }
}
