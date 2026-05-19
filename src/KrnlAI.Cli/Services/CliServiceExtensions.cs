using KrnlAI.Anticipation;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Abstractions.Mcp;
using KrnlAI.Core.Abstractions.Safety;
using KrnlAI.Core.Services.ExperimentTracking;
using KrnlAI.Core.Services.Memory;
using KrnlAI.Core.Services.ModelRegistry;
using KrnlAI.Core.Services.Safety;
using KrnlAI.Executive;
using KrnlAI.Infrastructure.InMemory;
using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.LLMGateway.Core.Services.Goals;
using KrnlAI.LLMGateway.Core.Services.Governance;
using KrnlAI.Memory;
using KrnlAI.Snapshot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        services.AddSingleton<ICognitiveHomeostasis>(_ => new CognitiveHomeostasisService());
        services.AddSingleton<IExecutiveController, ExecutiveController>();
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
        services.AddSingleton<ISchedulerService, KrnlAI.Infrastructure.Scheduling.InMemorySchedulerStore>();
        services.AddSingleton<ISafetyCaseStore, InMemorySafetyCaseStore>();
        services.AddSingleton<ISafetyAuditStore, InMemorySafetyAuditStore>();
        services.AddSingleton<ISafetyScenarioStore, InMemorySafetyScenarioStore>();
        services.AddSingleton<FundamentalRulesEngine>(sp =>
        {
            var logger = sp.GetService<ILogger<FundamentalRulesEngine>>();
            return new FundamentalRulesEngine(logger ?? NullLogger<FundamentalRulesEngine>.Instance);
        });
        services.AddSingleton<SafetyBenchRunner>(sp =>
        {
            var rules = sp.GetRequiredService<FundamentalRulesEngine>();
            var logger = sp.GetService<ILogger<SafetyBenchRunner>>();
            return new SafetyBenchRunner(rules, hybridEngine: null, logger: logger);
        });
        services.AddSingleton<IMcpServerRegistry>(new KrnlAI.Infrastructure.Mcp.McpServerRegistry(
            httpClientFactory: null,
            oauthHandler: null,
            logger: null));
        services.AddSingleton<IModelRegistry>(new InMemoryModelRegistry());
        services.AddSingleton<IExperimentTracker>(new InMemoryExperimentTracker());
        services.AddSingleton<InMemorySessionStore>();

        // Plugin local mode services
        services.AddSingleton<IAssemblyPluginLoader>(new KrnlAI.Infrastructure.Plugin.AssemblyPluginLoader());
        services.AddSingleton<IPluginDiscovery>(new KrnlAI.Infrastructure.Plugin.DirectoryPluginDiscovery());
        services.AddSingleton<IPluginSandbox, KrnlAI.Infrastructure.InMemory.InMemoryPluginSandbox>();
        services.AddSingleton<KrnlAI.Core.Services.Plugin.PluginHookManager>();
        services.AddSingleton(sp =>
        {
            var loader = sp.GetRequiredService<IAssemblyPluginLoader>();
            var discovery = sp.GetRequiredService<IPluginDiscovery>();
            var sandbox = sp.GetRequiredService<IPluginSandbox>();
            return new KrnlAI.Infrastructure.Plugin.PluginHost(discovery, loader, sandbox);
        });

        // Report generator for benchmark command
        services.AddSingleton<KrnlAI.Core.Abstractions.Safety.ISafetyReportGenerator>(
            new KrnlAI.Infrastructure.Reports.SafetyHtmlReportGenerator());

        return services;
    }
}
