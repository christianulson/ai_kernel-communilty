using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Commands;

[Trait("Category", "Unit")]
public sealed class DebugHandlersTests
{
    [Fact]
    public void DebugRunHandler_Create_ShouldReturnCommand()
    {
        var service = new VsDebugService(launchAction: () => { });
        var cmd = DebugRunHandler.Create(service);
        cmd.Name.Should().Be("debug-run");
        cmd.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DebugRunHandler_Handle_ShouldReturnSuccess()
    {
        var service = new VsDebugService(launchAction: () => { }, state: DebugState.Stopped);
        var cmd = DebugRunHandler.Create(service);

        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("launched");
    }

    [Fact]
    public async Task DebugRunHandler_WhenAlreadyRunning_ShouldReturnError()
    {
        var service = new VsDebugService(state: DebugState.Running);
        var cmd = DebugRunHandler.Create(service);

        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("already running");
    }

    [Fact]
    public void DebugStopHandler_Create_ShouldReturnCommand()
    {
        var service = new VsDebugService(stopAction: () => { });
        var cmd = DebugStopHandler.Create(service);
        cmd.Name.Should().Be("debug-stop");
    }

    [Fact]
    public async Task DebugStopHandler_Handle_ShouldReturnSuccess()
    {
        var service = new VsDebugService(stopAction: () => { }, state: DebugState.Running);
        var cmd = DebugStopHandler.Create(service);

        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("stopped");
    }

    [Fact]
    public async Task DebugStopHandler_WhenStopped_ShouldReturnMessage()
    {
        var service = new VsDebugService(state: DebugState.Stopped);
        var cmd = DebugStopHandler.Create(service);

        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("not running");
    }

    [Fact]
    public void DebugStepHandler_CreateStepOver_ShouldReturnCommand()
    {
        var service = new VsDebugService(state: DebugState.Running);
        var cmd = DebugStepHandler.CreateStepOver(service);
        cmd.Name.Should().Be("debug-step-over");
    }

    [Fact]
    public void DebugStepHandler_CreateStepInto_ShouldReturnCommand()
    {
        var service = new VsDebugService(state: DebugState.Running);
        var cmd = DebugStepHandler.CreateStepInto(service);
        cmd.Name.Should().Be("debug-step-into");
    }

    [Fact]
    public void DebugStepHandler_CreateContinue_ShouldReturnCommand()
    {
        var service = new VsDebugService(state: DebugState.Running);
        var cmd = DebugStepHandler.CreateContinue(service);
        cmd.Name.Should().Be("debug-continue");
    }

    [Fact]
    public async Task DebugStepHandler_WhenStopped_ShouldReturnMessage()
    {
        var service = new VsDebugService(state: DebugState.Stopped);
        var cmd = DebugStepHandler.CreateStepOver(service);

        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("not running");
    }

    [Fact]
    public void DebugBreakpointHandler_Create_ShouldReturnCommand()
    {
        var service = new VsDebugService(setBreakpointFunc: (f, l) => true);
        var cmd = DebugBreakpointHandler.Create(service);
        cmd.Name.Should().Be("debug-bp");
    }

    [Fact]
    public async Task DebugBreakpointHandler_WithValidArgs_ShouldSetBreakpoint()
    {
        var service = new VsDebugService(setBreakpointFunc: (f, l) =>
        {
            f.Should().Be(@"C:\test.cs");
            l.Should().Be(42);
            return true;
        });
        var cmd = DebugBreakpointHandler.Create(service);

        var result = await cmd.Handler(@"C:\test.cs:42", CancellationToken.None);

        result.Should().Contain("set");
    }

    [Fact]
    public async Task DebugBreakpointHandler_WithInvalidArgs_ShouldReturnUsage()
    {
        var service = new VsDebugService(setBreakpointFunc: (f, l) => true);
        var cmd = DebugBreakpointHandler.Create(service);

        var result = await cmd.Handler("invalid", CancellationToken.None);

        result.Should().Contain("Usage");
    }

    [Fact]
    public void DebugBuildHandler_Create_ShouldReturnCommand()
    {
        var service = new VsDebugService(buildAsync: _ => Task.FromResult("ok"));
        var cmd = DebugBuildHandler.Create(service);
        cmd.Name.Should().Be("debug-build");
    }

    [Fact]
    public async Task DebugBuildHandler_Handle_ShouldReturnBuildResult()
    {
        var service = new VsDebugService(buildAsync: _ => Task.FromResult("Build succeeded."));
        var cmd = DebugBuildHandler.Create(service);

        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("Build succeeded");
    }
}
