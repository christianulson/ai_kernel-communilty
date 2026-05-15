using System.CommandLine;
using AIKernel.Cli.Commands;
using AIKernel.Cli.Services;
using AIKernel.LLMGateway.Core.Abstractions;
using AIKernel.LLMGateway.Core.Services.Goals;
using AIKernel.LLMGateway.Core.Services.Governance;
using Kernel.Core.Abstractions;
using Kernel.Core.Services.Anticipation;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.Safety;
using Kernel.Core.Services.TemporalDepth;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class AnticipateCommandTests
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
        var sp = services.BuildServiceProvider();
        var ctx = new CliContext(sp);
        return (renderer, console, ctx);
    }

    [Fact]
    public async Task AnticipateCommand_WhenEmpty_ShouldShowNoProjections()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new AnticipateCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("anticipate").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("No active projections");
    }
}