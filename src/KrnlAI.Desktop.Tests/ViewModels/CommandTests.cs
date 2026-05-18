using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public class RelayCommandTests
{
    [Fact]
    public void Execute_ShouldInvokeAction()
    {
        bool executed = false;
        var cmd = new RelayCommand(() => executed = true);
        cmd.Execute(null);
        Assert.True(executed);
    }

    [Fact]
    public void Execute_ShouldPassParameter()
    {
        object? captured = null;
        var cmd = new RelayCommand(p => captured = p);
        cmd.Execute("test");
        Assert.Equal("test", captured);
    }

    [Fact]
    public void CanExecute_WithNoPredicate_ShouldReturnTrue()
    {
        var cmd = new RelayCommand(() => { });
        Assert.True(cmd.CanExecute(null));
    }

    [Fact]
    public void CanExecute_WithPredicate_ShouldReturnCorrect()
    {
        var cmd = new RelayCommand(() => { }, () => false);
        Assert.False(cmd.CanExecute(null));
    }

    [Fact]
    public void MultipleExecutes_ShouldAllWork()
    {
        int count = 0;
        var cmd = new RelayCommand(() => count++);
        cmd.Execute(null);
        cmd.Execute(null);
        cmd.Execute(null);
        Assert.Equal(3, count);
    }
}

public class AsyncRelayCommandTests
{
    [Fact]
    public async Task Execute_ShouldInvokeAsyncAction()
    {
        var tcs = new TaskCompletionSource();
        bool executed = false;
        var cmd = new AsyncRelayCommand(async () => { try { await Task.Yield(); executed = true; } finally { tcs.SetResult(); } });
        cmd.Execute(null);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(executed);
    }

    [Fact]
    public async Task Execute_ShouldPassParameter()
    {
        var tcs = new TaskCompletionSource();
        object? captured = null;
        var cmd = new AsyncRelayCommand(async p => { try { await Task.Yield(); captured = p; } finally { tcs.SetResult(); } });
        cmd.Execute("param");
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("param", captured);
    }

    [Fact]
    public void CanExecute_WithNoPredicate_ShouldReturnTrue()
    {
        Assert.True(new AsyncRelayCommand(() => Task.CompletedTask).CanExecute(null));
    }

    [Fact]
    public void CanExecute_WithPredicate_ShouldReturnFalse()
    {
        Assert.False(new AsyncRelayCommand(() => Task.CompletedTask, () => false).CanExecute(null));
    }

    [Fact]
    public void ConcurrentExecute_ShouldGuardReentrancy()
    {
        bool started = false;
        var reset = new TaskCompletionSource();
        var cmd = new AsyncRelayCommand(async () => { started = true; await reset.Task; });

        cmd.Execute(null);
        SpinWait.SpinUntil(() => started, 1000);
        cmd.Execute(null);

        reset.TrySetResult();
        Assert.True(started);
    }

    [Fact]
    public Task Execute_Exception_ShouldNotThrow()
    {
        var cmd = new AsyncRelayCommand(() => throw new InvalidOperationException("fail"));
        var ex = Record.Exception(() => cmd.Execute(null));
        Assert.Null(ex);
        return Task.Delay(30);
    }
}
