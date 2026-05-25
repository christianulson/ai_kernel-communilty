using System.CommandLine;
using KrnlAI.Cli.Commands;
using KrnlAI.Cli.Services;
using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.LLMGateway.Core.Services.Goals;
using KrnlAI.LLMGateway.Core.Services.Governance;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Services.Memory;
using KrnlAI.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Testing;
using Microsoft.Extensions.Options;
using KrnlAI.Anticipation;
using KrnlAI.Executive;
using KrnlAI.Memory;
using KrnlAI.Snapshot;

namespace KrnlAI.Cli.Tests;

public sealed class MemoryCommandTests
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
        services.AddSingleton<ICognitiveHomeostasis>(sp => new CognitiveHomeostasisService(Options.Create(new CognitiveHomeostasisOptions()), sp.GetRequiredService<ILogger<CognitiveHomeostasisService>>()));
        services.AddSingleton<IExecutiveController, ExecutiveController>();
        services.AddSingleton<IExecutiveStageBuilder, ExecutiveStageBuilder>();
        services.AddSingleton<IExecutiveModeSelector, ExecutiveModeSelector>();
        services.AddSingleton<IAnticipationStore, InMemoryAnticipationStore>();
        services.AddSingleton<IProspectiveMemoryStore, InMemoryProspectiveMemoryStore>();
        services.AddSingleton<IProspectiveMemoryService>(sp =>
            new ProspectiveMemoryService(sp.GetRequiredService<IProspectiveMemoryStore>()));
        services.AddSingleton<IAnticipationService>(sp =>
            new AnticipationService(Enumerable.Empty<IProjectionSource>(), sp.GetRequiredService<IAnticipationStore>()));
        services.AddSingleton<IGoalStore, InMemoryGoalStore>();
        services.AddSingleton<ISafetyCaseStore, InMemorySafetyCaseStore>();
        services.AddSingleton<FundamentalRulesEngine>();
        services.AddSingleton<KrnlAI.Core.Abstractions.Mcp.IMcpServerRegistry>(new KrnlAI.Infrastructure.Mcp.McpServerRegistry());
        services.AddSingleton<KrnlAI.Core.Services.ModelRegistry.IModelRegistry>(new KrnlAI.Infrastructure.InMemory.InMemoryModelRegistry());
        services.AddSingleton<KrnlAI.Core.Services.ExperimentTracking.IExperimentTracker>(new KrnlAI.Infrastructure.InMemory.InMemoryExperimentTracker());
        var sp = services.BuildServiceProvider();
        var ctx = new CliContext(sp);
        return (renderer, console, ctx);
    }

    [Fact]
    public async Task MemoryCommand_Search_ShouldReturnMoments()
    {
        var (renderer, console, ctx) = Setup();
        var seeder = new CliSeeder(ctx.MomentStore, ctx.MomentClassifierStore);
        await seeder.SeedAsync();
        var cmd = new MemoryCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("memory search mom").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("mom-0001");
    }

    [Fact]
    public async Task MemoryCommand_Working_ShouldShowCognitiveState()
    {
        var (renderer, console, ctx) = Setup();
        var cmd = new MemoryCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("memory working").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("Status:");
        output.Should().Contain("Health Score");
    }

    [Fact]
    public async Task MemoryCommand_Search_WithCategoryFilter_ShouldFilterByCategory()
    {
        var (renderer, console, ctx) = Setup();
        var seeder = new CliSeeder(ctx.MomentStore, ctx.MomentClassifierStore);
        await seeder.SeedAsync();
        var cmd = new MemoryCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("memory search --category Anomaly").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("mom-0003");
    }

    [Fact]
    public async Task MemoryCommand_Search_WithCategoryFilter_ShouldExcludeOtherCategories()
    {
        var (renderer, console, ctx) = Setup();
        var seeder = new CliSeeder(ctx.MomentStore, ctx.MomentClassifierStore);
        await seeder.SeedAsync();
        var cmd = new MemoryCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("memory search --category Anomaly").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().NotContain("mom-0001");
        output.Should().NotContain("mom-0002");
    }

    [Fact]
    public async Task MemoryCommand_Search_WithQuery_ShouldFilterByText()
    {
        var (renderer, console, ctx) = Setup();
        var seeder = new CliSeeder(ctx.MomentStore, ctx.MomentClassifierStore);
        await seeder.SeedAsync();
        var cmd = new MemoryCommand(ctx, renderer).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("memory search mom-0002").InvokeAsync();

        result.Should().Be(0);
        var output = console.Output;
        output.Should().Contain("mom-0002");
    }
}