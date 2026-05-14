using System.CommandLine;
using AIKernel.Cli.Commands;
using AIKernel.Cli.Services;
using Kernel.Core.Abstractions;
using Kernel.Core.Services.Anticipation;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.TemporalDepth;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class StatusCommandTests
{
    private static (ConsoleRenderer Renderer, TestConsole Console, CliContext Ctx) Setup()
    {
        var console = new TestConsole();
        var renderer = new ConsoleRenderer(console);
        var services = new ServiceCollection();
        services.AddSingleton<IMomentStore, InMemoryMomentStore>();
        services.AddSingleton<IMomentClassifierStore, InMemoryMomentClassifierStore>();
        services.AddSingleton<IArchiveStore>(_ => new InMemoryArchiveStore<object>("test-archive"));
        services.AddSingleton<ICognitiveSnapshotService, InMemorySnapshotStore>();
        services.AddSingleton<ICognitiveHomeostasis>(_ => new CognitiveHomeostasisService());
        services.AddSingleton<IExecutiveController, ExecutiveController>();
        services.AddSingleton<IAnticipationStore, InMemoryAnticipationStore>();
        services.AddSingleton<IProspectiveMemoryStore, InMemoryProspectiveMemoryStore>();
        services.AddSingleton<IProspectiveMemoryService>(sp =>
            new ProspectiveMemoryService(sp.GetRequiredService<IProspectiveMemoryStore>()));
        services.AddSingleton<IAnticipationService>(sp =>
            new AnticipationService(
                Enumerable.Empty<Kernel.Core.Abstractions.IProjectionSource>(),
                sp.GetRequiredService<IAnticipationStore>()));
        var sp = services.BuildServiceProvider();
        var ctx = new CliContext(sp);
        return (renderer, console, ctx);
    }

    [Fact]
    public async Task StatusCommand_WithDefaultOptions_ShouldShowBasicInfo()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new StatusCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("status").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("Status:");
        output.Should().Contain("Health Score");
    }

    [Fact]
    public async Task StatusCommand_WithVerbose_ShouldShowFatigueAndStarvation()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new StatusCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("status --verbose").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("Fatigue:");
        output.Should().Contain("Starvation:");
        output.Should().Contain("Sleep Pressure:");
    }
}
