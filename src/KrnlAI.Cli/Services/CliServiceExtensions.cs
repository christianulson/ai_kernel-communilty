using KrnlAI.Anticipation;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Abstractions.Mcp;
using KrnlAI.Core.Abstractions.Safety;
using KrnlAI.Core.Services.ExperimentTracking;
using KrnlAI.Core.Services.Memory;
using KrnlAI.Core.Services.ModelRegistry;
using KrnlAI.Core.Services.Safety;
using KrnlAI.Executive;
using KrnlAI.Infrastructure;
using KrnlAI.Infrastructure.InMemory;
using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.LLMGateway.Core.Services.Goals;
using KrnlAI.LLMGateway.Core.Services.Governance;
using KrnlAI.Memory;
using KrnlAI.Snapshot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KrnlAI.Cli.Services;

public static class CliServiceExtensions
{
    public static IServiceCollection AddCliServices(this IServiceCollection services)
    {
        services.AddSingleton<CliContext>();
        services.AddSingleton<IMomentStore, InMemoryMomentStore>();
        services.AddSingleton<IMomentClassifierStore, InMemoryMomentClassifierStore>();
        services.AddSingleton<IArchiveStore>(_ => new InMemoryArchiveStore<object>("cli-archive"));
        services.AddSingleton<ICognitiveSnapshotService, InMemorySnapshotStore>();
        services.AddSingleton<ICognitiveHomeostasis>(sp =>
            new CognitiveHomeostasisService(
                Options.Create(new CognitiveHomeostasisOptions()),
                sp.GetRequiredService<ILogger<CognitiveHomeostasisService>>()));
        services.AddSingleton<IExecutiveStageBuilder>(sp =>
            new ExecutiveStageBuilder(sp.GetRequiredService<ILogger<ExecutiveStageBuilder>>()));
        services.AddSingleton<IExecutiveModeSelector>(sp =>
            new ExecutiveModeSelector(sp.GetRequiredService<ILogger<ExecutiveModeSelector>>()));
        services.AddSingleton<IExecutiveController>(sp =>
            new ExecutiveController(
                sp.GetRequiredService<ICognitiveHomeostasis>(),
                sp.GetRequiredService<IExecutiveStageBuilder>(),
                sp.GetRequiredService<IExecutiveModeSelector>(),
                sp.GetRequiredService<ILogger<ExecutiveController>>()));
        services.AddSingleton<IAnticipationStore, InMemoryAnticipationStore>();
        services.AddSingleton<IProspectiveMemoryStore, InMemoryProspectiveMemoryStore>();
        services.AddSingleton<IProspectiveMemoryService>(sp =>
            new ProspectiveMemoryService(
                sp.GetRequiredService<IProspectiveMemoryStore>(),
                eventBus: null,
                logger: null));
        services.AddSingleton<IAnticipationService>(sp =>
            new AnticipationService(
                Enumerable.Empty<IProjectionSource>(),
                sp.GetRequiredService<IAnticipationStore>(),
                logger: null));
        services.AddSingleton<IGoalStore, InMemoryGoalStore>();
        services.AddSingleton<ISchedulerService, Infrastructure.Scheduling.InMemorySchedulerStore>();
        services.AddSingleton<ISafetyCaseStore, InMemorySafetyCaseStore>();
        services.AddSingleton<ISafetyAuditStore, InMemorySafetyAuditStore>();
        services.AddSingleton<ISafetyScenarioStore, InMemorySafetyScenarioStore>();
        services.AddSingleton(sp =>
        {
            var logger = sp.GetService<ILogger<FundamentalRulesEngine>>();
            return new FundamentalRulesEngine(logger ?? NullLogger<FundamentalRulesEngine>.Instance);
        });
        services.AddSingleton(sp =>
        {
            var rules = sp.GetRequiredService<FundamentalRulesEngine>();
            var logger = sp.GetService<ILogger<SafetyBenchRunner>>();
            return new SafetyBenchRunner(rules, hybridEngine: null, logger: logger);
        });
        services.AddSingleton<IMcpServerRegistry>(new Infrastructure.Mcp.McpServerRegistry(
            httpClientFactory: null,
            oauthHandler: null,
            logger: null));
        services.AddSingleton<IModelRegistry>(new InMemoryModelRegistry());
        services.AddSingleton<IExperimentTracker>(new InMemoryExperimentTracker());
        services.AddSingleton<InMemorySessionStore>();

        // Plugin local mode services
        services.AddSingleton<IAssemblyPluginLoader>(new Infrastructure.Plugin.AssemblyPluginLoader());
        services.AddSingleton<IPluginDiscovery>(new Infrastructure.Plugin.DirectoryPluginDiscovery());
        services.AddSingleton<IPluginSandbox, InMemoryPluginSandbox>();
        services.AddSingleton<Core.Services.Plugin.PluginHookManager>();
        services.AddSingleton(sp =>
        {
            var loader = sp.GetRequiredService<IAssemblyPluginLoader>();
            var discovery = sp.GetRequiredService<IPluginDiscovery>();
            var sandbox = sp.GetRequiredService<IPluginSandbox>();
            return new Infrastructure.Plugin.PluginHost(discovery, loader, sandbox);
        });

        // Report generator for benchmark command
        services.AddSingleton<ISafetyReportGenerator, Infrastructure.Reports.SafetyHtmlReportGenerator>();

        // Plan/Act mode
        services.AddSingleton<IPlanArtifactStore, InMemoryPlanArtifactStore>();
        services.AddSingleton<KrnlAI.Cognition.Runtime.PlanActOrchestrator>();

        // Checkpoints
        services.AddSingleton<ICheckpointStore, InMemoryCheckpointStore>();
        services.AddSingleton<ICheckpointManager, Core.Services.Versioning.CheckpointManager>();

        // Kanban
        services.AddSingleton<KanbanRenderer>();
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IKanbanService, KanbanService>();

        return services;
    }
}
