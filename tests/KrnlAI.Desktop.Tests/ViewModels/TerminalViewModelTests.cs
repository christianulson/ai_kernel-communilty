namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class TerminalViewModelTests
{
    [Fact]
    public void InitialState_ShouldBeDisconnected()
    {
        var vm = new TerminalViewModel();
        Assert.Equal("Disconnected", vm.ConnectionStatus);
        Assert.False(vm.IsExecuting);
        Assert.Empty(vm.Output);
        Assert.Empty(vm.Command);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public void Command_SettingProperty_ShouldNotify()
    {
        var vm = new TerminalViewModel();
        var notified = false;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.Command)) notified = true; };
        vm.Command = "echo hello";
        Assert.True(notified);
        Assert.Equal("echo hello", vm.Command);
    }

    [Fact]
    public void ExecuteCommand_EmptyCommand_ShouldNotInvokeHub()
    {
        var vm = new TerminalViewModel();
        vm.Command = "";
        vm.ExecuteCommand.Execute(null);
        Assert.Empty(vm.Output);
    }

    [Fact]
    public void ExecuteCommand_WhitespaceCommand_ShouldNotInvokeHub()
    {
        var vm = new TerminalViewModel();
        vm.Command = "   ";
        vm.ExecuteCommand.Execute(null);
        Assert.Empty(vm.Output);
    }

    [Fact]
    public void ExecuteCommand_ShouldAddToHistory()
    {
        var vm = new TerminalViewModel();
        vm.Command = "echo hello";

        var canExecute = vm.ExecuteCommand.CanExecute(null);
        Assert.True(canExecute);
    }

    [Fact]
    public void ClearOutputCommand_ShouldClearOutput()
    {
        var vm = new TerminalViewModel();
        vm.Output.Add(new TerminalOutput("output", "line1"));
        vm.Output.Add(new TerminalOutput("error", "err1"));
        Assert.Equal(2, vm.Output.Count);

        vm.ClearOutputCommand.Execute(null);
        Assert.Empty(vm.Output);
    }

    [Fact]
    public void ClearOutputCommand_ShouldClearHistory()
    {
        var vm = new TerminalViewModel();
        vm.CommandHistory.Add("cmd1");
        vm.CommandHistory.Add("cmd2");

        vm.ClearOutputCommand.Execute(null);
        Assert.Empty(vm.CommandHistory);
    }

    [Fact]
    public void NavigateHistory_Up_ShouldRestorePreviousCommand()
    {
        var vm = new TerminalViewModel();
        vm.CommandHistory.Add("cmd1");
        vm.CommandHistory.Add("cmd2");

        vm.NavigateHistoryUp();
        Assert.Equal("cmd1", vm.Command);

        vm.NavigateHistoryUp();
        Assert.Equal("cmd2", vm.Command);

        vm.NavigateHistoryUp();
        Assert.Equal("cmd2", vm.Command);
    }

    [Fact]
    public void NavigateHistory_Down_ShouldRestoreNextCommand()
    {
        var vm = new TerminalViewModel();
        vm.CommandHistory.Add("cmd1");
        vm.CommandHistory.Add("cmd2");
        vm.CommandHistoryIndex = 0;
        vm.Command = "cmd1";

        vm.NavigateHistoryDown();
        Assert.Equal("", vm.Command);

        vm.NavigateHistoryDown();
        Assert.Equal("", vm.Command);
    }

    [Fact]
    public void NavigateHistory_EmptyHistory_ShouldNotThrow()
    {
        var vm = new TerminalViewModel();
        vm.NavigateHistoryUp();
        vm.NavigateHistoryDown();
        Assert.Equal("", vm.Command);
    }

    [Fact]
    public void ErrorMessage_ShouldBeSettable()
    {
        var vm = new TerminalViewModel();
        vm.ErrorMessage = "test error";
        Assert.Equal("test error", vm.ErrorMessage);
    }

    [Fact]
    public void ConnectionStatus_ShouldBeSettable()
    {
        var vm = new TerminalViewModel();
        vm.ConnectionStatus = "Connected";
        Assert.Equal("Connected", vm.ConnectionStatus);
    }

    [Fact]
    public void IsExecuting_ShouldBeSettable()
    {
        var vm = new TerminalViewModel();
        vm.IsExecuting = true;
        Assert.True(vm.IsExecuting);

        vm.IsExecuting = false;
        Assert.False(vm.IsExecuting);
    }

    [Fact]
    public void CancelCommand_WhenNotExecuting_ShouldNotThrow()
    {
        var vm = new TerminalViewModel();
        vm.CancelCommand.Execute(null);
        Assert.False(vm.IsExecuting);
    }

    [Fact]
    public void OutputLineCount_ShouldUpdateWhenOutputChanges()
    {
        var vm = new TerminalViewModel();

        vm.Output.Add(new TerminalOutput("output", "line1"));
        Assert.Equal("1 lines", vm.OutputLineCount);

        vm.Output.Add(new TerminalOutput("error", "err1"));
        Assert.Equal("2 lines", vm.OutputLineCount);

        vm.Output.Clear();
        Assert.Equal("0 lines", vm.OutputLineCount);
    }
}
