using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Prompts;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Prompts;

public sealed class AnalyzePromptBuilderTests
{
    [Fact]
    public void BuildCodePrompt_ValidInput_ShouldMatchLegacyFormat()
    {
        var prompt = AnalyzePromptBuilder.BuildCodePrompt("CSharp", "Program.cs", "var x = 1;");

        prompt.Should().Be(
            "Analyze this CSharp from Program.cs:\n\n```CSharp\nvar x = 1;\n```");
    }

    [Fact]
    public void BuildCodePrompt_EmptyCode_ShouldStillWrapInFence()
    {
        var prompt = AnalyzePromptBuilder.BuildCodePrompt("CSharp", "Program.cs", "");

        prompt.Should().Be("Analyze this CSharp from Program.cs:\n\n```CSharp\n\n```");
    }

    [Fact]
    public void BuildErrorPrompt_ValidInput_ShouldMatchLegacyFormat()
    {
        var prompt = AnalyzePromptBuilder.BuildErrorPrompt("CS0103 not found", "Program.cs", 42);

        prompt.Should().Be(
            "Analyze this build error and suggest a fix:\n\nError: CS0103 not found\nFile: Program.cs\nLine: 42");
    }

    [Fact]
    public void BuildErrorPrompt_NullFile_ShouldWriteUnknown()
    {
        var prompt = AnalyzePromptBuilder.BuildErrorPrompt("boom", null, 1);

        prompt.Should().Be("Analyze this build error and suggest a fix:\n\nError: boom\nFile: unknown\nLine: 1");
    }
}