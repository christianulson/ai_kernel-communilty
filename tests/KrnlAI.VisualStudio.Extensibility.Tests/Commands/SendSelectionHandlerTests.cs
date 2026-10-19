using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Commands;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Commands;

public sealed class SendSelectionHandlerTests
{
    private sealed class FakeClipboard : IClipboard
    {
        public string? LastSet { get; private set; }

        public Task SetTextAsync(string text, CancellationToken cancellationToken)
        {
            LastSet = text;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SendSelection_ValidSelection_ShouldCopyPromptToClipboard()
    {
        var clipboard = new FakeClipboard();
        var handler = new SendSelectionHandler(clipboard);

        await handler.SendSelectionAsync("CSharp", "Program.cs", "var x = 1;", CancellationToken.None);

        clipboard.LastSet.Should().Be(
            "Analyze this CSharp from Program.cs:\n\n```CSharp\nvar x = 1;\n```");
    }

    [Fact]
    public async Task SendSelection_EmptySelection_ShouldNotTouchClipboard()
    {
        var clipboard = new FakeClipboard();
        var handler = new SendSelectionHandler(clipboard);

        await handler.SendSelectionAsync("CSharp", "Program.cs", "", CancellationToken.None);

        clipboard.LastSet.Should().BeNull();
    }
}