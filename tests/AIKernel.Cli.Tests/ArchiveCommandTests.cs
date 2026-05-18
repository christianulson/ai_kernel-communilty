using System.CommandLine;
using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Services;
using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.LLMGateway.Core.Services.Goals;
using KrnlAI.LLMGateway.Core.Services.Governance;
using Kernel.Core.Abstractions;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;
using Kernel.Anticipation;
using Kernel.Executive;
using Kernel.Memory;
using Kernel.Snapshot;

namespace KrnlAI.Cli.Tests;

public sealed class ArchiveCommandTests
{
    private static (ConsoleRenderer Renderer, TestConsole Console, CliContext Ctx) Setup()
    {
        var console = new TestConsole();
        var renderer = new ConsoleRenderer(console);
        var services = new ServiceCollection();
        services.AddLogging();
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
            new AnticipationService(Enumerable.Empty<IProjectionSource>(), sp.GetRequiredService<IAnticipationStore>()));
        services.AddSingleton<IGoalStore, InMemoryGoalStore>();
        services.AddSingleton<ISafetyCaseStore, InMemorySafetyCaseStore>();
        services.AddSingleton<FundamentalRulesEngine>();
        services.AddSingleton<Kernel.Core.Abstractions.Mcp.IMcpServerRegistry>(new Kernel.Infrastructure.Mcp.McpServerRegistry());
        services.AddSingleton<Kernel.Core.Services.ModelRegistry.IModelRegistry>(new Kernel.Core.Services.ModelRegistry.InMemoryModelRegistry());
        services.AddSingleton<Kernel.Core.Services.ExperimentTracking.IExperimentTracker>(new Kernel.Core.Services.ExperimentTracking.InMemoryExperimentTracker());
        var sp = services.BuildServiceProvider();
        var ctx = new CliContext(sp);
        return (renderer, console, ctx);
    }

    [Fact]
    public async Task ArchiveCommand_List_WhenEmpty_ShouldShowNoEntries()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new ArchiveCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("archive list").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("No archived entries");
    }

    [Fact]
    public async Task ArchiveCommand_Count_ShouldShowZero()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new ArchiveCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("archive count").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("0");
    }

    [Fact]
    public async Task ArchiveCommand_Purge_ShouldSucceed()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new ArchiveCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("archive purge").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Purged 0");
    }
}