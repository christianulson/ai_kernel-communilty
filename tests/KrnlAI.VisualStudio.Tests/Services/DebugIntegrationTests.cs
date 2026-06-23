using KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;
using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

[Trait("Category", "Unit")]
public sealed class DebugIntegrationTests
{
    [Fact]
    public void DebugRunHandler_MapsToSlashCommand()
    {
        var service = new VsDebugService(launchAction: () => { }, state: DebugState.Stopped);
        var cmd = DebugRunHandler.Create(service);
        cmd.Name.Should().Be("debug-run");
    }

    [Fact]
    public async Task DebugRunHandler_ExecutesViaService()
    {
        var launched = false;
        var service = new VsDebugService(
            launchAction: () => launched = true,
            state: DebugState.Stopped);
        var cmd = DebugRunHandler.Create(service);

        var result = await cmd.Handler("MyApp", CancellationToken.None);

        launched.Should().BeTrue();
        result.Should().Contain("launched");
    }

    [Fact]
    public void AllDebugCommands_AreRegistered()
    {
        var tracker = new VsOperationTracker();
        var service = new VsDebugService(
            launchAction: () => { },
            stopAction: () => { },
            stepOverAction: () => { },
            stepIntoAction: () => { },
            continueAction: () => { },
            setBreakpointFunc: (f, l) => true,
            buildAsync: _ => Task.FromResult("ok"),
            state: DebugState.Stopped);

        var router = new SlashCommandRouter(
            new FakeKernelClient(),
            new FakeSolutionContext(),
            new FakeApplyEdit(),
            new FakeAgenticLoop(),
            debugTracker: tracker,
            debugService: service);

        // All debug commands should be resolvable
        router.Resolve("/debug").Should().NotBeNull();
        router.Resolve("/debug-run").Should().NotBeNull();
        router.Resolve("/debug-stop").Should().NotBeNull();
        router.Resolve("/debug-step-over").Should().NotBeNull();
        router.Resolve("/debug-step-into").Should().NotBeNull();
        router.Resolve("/debug-continue").Should().NotBeNull();
        router.Resolve("/debug-bp").Should().NotBeNull();
        router.Resolve("/debug-build").Should().NotBeNull();
    }

    [Fact]
    public async Task DebugRunHandler_WhenStopped_TransitionsToRunning()
    {
        var service = new VsDebugService(launchAction: () => { }, state: DebugState.Stopped);
        var cmd = DebugRunHandler.Create(service);

        await cmd.Handler("", CancellationToken.None);

        service.State.Should().Be(DebugState.Running);
    }

    [Fact]
    public async Task DebugStopHandler_AfterLaunch_TransitionsToStopped()
    {
        var service = new VsDebugService(
            launchAction: () => { },
            stopAction: () => { },
            state: DebugState.Stopped);
        var runCmd = DebugRunHandler.Create(service);
        var stopCmd = DebugStopHandler.Create(service);

        await runCmd.Handler("", CancellationToken.None);
        await stopCmd.Handler("", CancellationToken.None);

        service.State.Should().Be(DebugState.Stopped);
    }

    [Fact]
    public async Task DebugStepHandler_RequiresRunningState()
    {
        var service = new VsDebugService(state: DebugState.Stopped);
        var cmd = DebugStepHandler.CreateStepOver(service);

        var result = await cmd.Handler("", CancellationToken.None);

        result.Should().Contain("not running");
    }

    [Fact]
    public async Task DebugFullCycle_IsTracked()
    {
        var tracker = new VsOperationTracker();
        var service = new VsDebugService(
            debugTracker: tracker,
            launchAction: () => { },
            stopAction: () => { },
            stepOverAction: () => { },
            buildAsync: _ => Task.FromResult("ok"),
            state: DebugState.Stopped);

        var runCmd = DebugRunHandler.Create(service);
        var stopCmd = DebugStopHandler.Create(service);
        var stepCmd = DebugStepHandler.CreateStepOver(service);
        var buildCmd = DebugBuildHandler.Create(service);

        await buildCmd.Handler("", CancellationToken.None);
        await runCmd.Handler("", CancellationToken.None);
        await stepCmd.Handler("", CancellationToken.None);
        await stopCmd.Handler("", CancellationToken.None);

        tracker.History.Should().HaveCount(4);
        tracker.History[0].Name.Should().Be("debug.build");
        tracker.History[1].Name.Should().Be("debug.launch");
        tracker.History[2].Name.Should().Be("debug.step_over");
        tracker.History[3].Name.Should().Be("debug.stop");
    }
}
