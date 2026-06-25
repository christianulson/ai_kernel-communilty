using KrnlAI.Desktop.Infrastructure.KernelClient;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class SafeCallTests
{
    [Fact]
    public async Task ExecuteAsync_Success_ShouldReturnValue()
    {
        var result = await SafeCall.ExecuteAsync(() => Task.FromResult(42), -1).ConfigureAwait(false);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_Exception_ShouldReturnDefault()
    {
        var result = await SafeCall.ExecuteAsync(() => throw new InvalidOperationException(), -1).ConfigureAwait(false);
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Task_ShouldComplete()
    {
        var called = false;
        await SafeCall.ExecuteAsync(() => { called = true; return Task.CompletedTask; }).ConfigureAwait(false);
        Assert.True(called);
    }

    [Fact]
    public async Task ExecuteAsync_Task_Exception_ShouldNotThrow()
    {
        var ex = await Record.ExceptionAsync(() =>
            SafeCall.ExecuteAsync(() => throw new InvalidOperationException())).ConfigureAwait(false);
        Assert.Null(ex);
    }
}
