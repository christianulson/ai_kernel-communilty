using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

[Trait("Category", "Unit")]
public sealed class VsDebugServiceTests
{
    [Fact]
    public void Constructor_ShouldSetInitialState()
    {
        var service = new VsDebugService();

        service.State.Should().Be(DebugState.Stopped);
    }

    [Fact]
    public async Task BuildSolutionAsync_ShouldReturnBuildOutput()
    {
        var service = new VsDebugService(buildAsync: _ => Task.FromResult("Build succeeded. 0 errors, 0 warnings"));

        var result = await service.BuildSolutionAsync();

        result.Should().Contain("Build succeeded");
    }

    [Fact]
    public async Task BuildSolutionAsync_WithErrors_ShouldReturnErrors()
    {
        var service = new VsDebugService(buildAsync: _ => Task.FromResult("Build failed. 2 errors"));

        var result = await service.BuildSolutionAsync();

        result.Should().Contain("Build failed");
    }

    [Fact]
    public async Task LaunchProjectAsync_WhenStopped_ShouldReturnTrue()
    {
        var launched = false;
        var service = new VsDebugService(
            launchAction: () => launched = true,
            state: DebugState.Stopped);

        var result = await service.LaunchProjectAsync();

        result.Should().BeTrue();
        launched.Should().BeTrue();
        service.State.Should().Be(DebugState.Running);
    }

    [Fact]
    public async Task LaunchProjectAsync_WhenAlreadyRunning_ShouldReturnFalse()
    {
        var service = new VsDebugService(state: DebugState.Running);

        var result = await service.LaunchProjectAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_WhenRunning_ShouldStopAndChangeState()
    {
        var stopped = false;
        var service = new VsDebugService(
            stopAction: () => stopped = true,
            state: DebugState.Running);

        await service.StopAsync();

        stopped.Should().BeTrue();
        service.State.Should().Be(DebugState.Stopped);
    }

    [Fact]
    public async Task StopAsync_WhenStopped_ShouldNotThrow()
    {
        var service = new VsDebugService(state: DebugState.Stopped);

        await service.StopAsync();

        service.State.Should().Be(DebugState.Stopped);
    }

    [Fact]
    public async Task StepOverAsync_WhenRunning_ShouldExecute()
    {
        var executed = false;
        var service = new VsDebugService(
            stepOverAction: () => executed = true,
            state: DebugState.Running);

        await service.StepOverAsync();

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task StepOverAsync_WhenStopped_ShouldNotExecute()
    {
        var executed = false;
        var service = new VsDebugService(
            stepOverAction: () => executed = true,
            state: DebugState.Stopped);

        await service.StepOverAsync();

        executed.Should().BeFalse();
    }

    [Fact]
    public async Task StepIntoAsync_WhenRunning_ShouldExecute()
    {
        var executed = false;
        var service = new VsDebugService(
            stepIntoAction: () => executed = true,
            state: DebugState.Running);

        await service.StepIntoAsync();

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ContinueAsync_WhenRunning_ShouldExecute()
    {
        var executed = false;
        var service = new VsDebugService(
            continueAction: () => executed = true,
            state: DebugState.Running);

        await service.ContinueAsync();

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task SetBreakpointAsync_ShouldReturnTrue()
    {
        var service = new VsDebugService(
            setBreakpointFunc: (file, line) =>
            {
                file.Should().Be(@"C:\test\Program.cs");
                line.Should().Be(42);
                return true;
            });

        var result = await service.SetBreakpointAsync(@"C:\test\Program.cs", 42);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveBreakpointAsync_ShouldReturnTrue()
    {
        var removed = false;
        var service = new VsDebugService(
            removeBreakpointFunc: (file, line) =>
            {
                removed = true;
                return true;
            });

        var result = await service.RemoveBreakpointAsync(@"C:\test\Program.cs", 42);

        result.Should().BeTrue();
        removed.Should().BeTrue();
    }

    [Fact]
    public async Task StateChanged_ShouldFireOnLaunch()
    {
        DebugState? captured = null;
        var service = new VsDebugService(
            launchAction: () => { },
            state: DebugState.Stopped);
        service.StateChanged += s => captured = s;

        await service.LaunchProjectAsync();

        captured.Should().Be(DebugState.Running);
    }

    [Fact]
    public async Task StateChanged_ShouldFireOnStop()
    {
        DebugState? captured = null;
        var service = new VsDebugService(
            launchAction: () => { },
            stopAction: () => { },
            state: DebugState.Running);
        service.StateChanged += s => captured = s;

        await service.StopAsync();

        captured.Should().Be(DebugState.Stopped);
    }

    [Fact]
    public async Task LaunchProjectAsync_ShouldTrackOperation()
    {
        var tracker = new VsOperationTracker();
        var service = new VsDebugService(
            debugTracker: tracker,
            launchAction: () => { },
            state: DebugState.Stopped);

        await service.LaunchProjectAsync();

        tracker.History.Should().HaveCount(1);
        tracker.History[0].Name.Should().Be("debug.launch");
    }

    [Fact]
    public async Task BuildSolutionAsync_ShouldTrackOperation()
    {
        var tracker = new VsOperationTracker();
        var service = new VsDebugService(
            debugTracker: tracker,
            buildAsync: _ => Task.FromResult("ok"));

        await service.BuildSolutionAsync();

        tracker.History.Should().HaveCount(1);
        tracker.History[0].Name.Should().Be("debug.build");
    }

    [Fact]
    public async Task SetBreakpointAsync_ShouldTrackOperation()
    {
        var tracker = new VsOperationTracker();
        var service = new VsDebugService(
            debugTracker: tracker,
            setBreakpointFunc: (f, l) => true);

        await service.SetBreakpointAsync(@"C:\test.cs", 10);

        tracker.History.Should().HaveCount(1);
        tracker.History[0].Name.Should().Be("debug.breakpoint.set");
    }

    [Fact]
    public async Task FullDebugCycle_ShouldTrackAllSteps()
    {
        var tracker = new VsOperationTracker();
        var launchCalled = false;
        var stepOverCalled = false;
        var stopCalled = false;

        var service = new VsDebugService(
            debugTracker: tracker,
            launchAction: () => launchCalled = true,
            stepOverAction: () => stepOverCalled = true,
            stopAction: () => stopCalled = true,
            state: DebugState.Stopped);

        await service.LaunchProjectAsync();
        launchCalled.Should().BeTrue();

        await service.StepOverAsync();
        stepOverCalled.Should().BeTrue();

        await service.StopAsync();
        stopCalled.Should().BeTrue();

        tracker.History.Should().HaveCount(3);
        tracker.History[0].Name.Should().Be("debug.launch");
        tracker.History[1].Name.Should().Be("debug.step_over");
        tracker.History[2].Name.Should().Be("debug.stop");
    }
}
