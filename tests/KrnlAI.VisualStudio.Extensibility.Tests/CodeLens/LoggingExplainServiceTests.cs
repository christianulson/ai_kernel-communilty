using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.CodeLens;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.CodeLens;

public sealed class LoggingExplainServiceTests
{
    [Fact]
    public async Task ExplainAsync_ValidIdentifier_ShouldCompleteWithoutThrowing()
    {
        var service = new LoggingExplainService();

        var act = async () => await service.ExplainAsync("Namespace.Type.Method", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}