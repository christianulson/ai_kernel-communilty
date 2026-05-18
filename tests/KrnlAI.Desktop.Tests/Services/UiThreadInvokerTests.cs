using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class UiThreadInvokerTests
{
    [Fact]
    public void UiThreadInvoker_InvokeWithoutApplication_ShouldRunAction()
    {
        var called = false;

        UiThreadInvoker.Invoke(() => called = true);

        Assert.True(called);
    }
}
