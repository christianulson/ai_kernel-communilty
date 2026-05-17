using System.CommandLine;
using AIKernel.Cli.Commands;
using AIKernel.Cli.Services;
using AIKernel.LLMGateway.Core.Abstractions;
using AIKernel.LLMGateway.Core.Services.Goals;
using AIKernel.LLMGateway.Core.Services.Governance;
using Kernel.Core.Abstractions;
using Kernel.Core.Abstractions.Mcp;
using Kernel.Core.Services.Anticipation;
using Kernel.Core.Services.ExperimentTracking;
using Kernel.Core.Services.ModelRegistry;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.Safety;
using Kernel.Core.Services.TemporalDepth;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Testing;

namespace AIKernel.Cli.Tests;

public sealed class IntentionsCommandTests
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
        services.AddSingleton<IProspectiveMemoryStore>(_ =>
        {
            var store = new InMemoryProspectiveMemoryStore();
            store.StoreAsync(new Intention(
                "int-001", "Test intention",
                new IntentionTrigger(IntentionTriggerType.AfterDelay, null, TimeSpan.FromMinutes(5), null),
                "{}", "test", 0.7, DateTimeOffset.UtcNow, null, IntentionStatus.Pending),
                CancellationToken.None).GetAwaiter().GetResult();
            return store;
        });
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
    public async Task IntentionsCommand_ShouldShowPendingIntentions()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new IntentionsCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("intentions").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("int-001");
        output.Should().Contain("Test");
    }

    [Fact]
    public async Task IntentionsCommand_WithDomainFilter_ShouldFilter()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new IntentionsCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("intentions --domain nonexistent").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("No pending intentions");
    }
}