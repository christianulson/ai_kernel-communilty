using AIKernel.LLMGateway.Core.Abstractions;
using AIKernel.LLMGateway.Core.Services.Goals;
using AIKernel.LLMGateway.Core.Services.Governance;
using Kernel.Core.Abstractions;
using Kernel.Core.Abstractions.Mcp;
using Kernel.Core.Services.Anticipation;
using Kernel.Core.Services.ExperimentTracking;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.ModelRegistry;
using Kernel.Core.Services.Safety;
using Kernel.Core.Services.TemporalDepth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIKernel.Cli.Services;

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
        services.AddSingleton<ISafetyCaseStore, InMemorySafetyCaseStore>();
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
        services.AddSingleton<IMcpServerRegistry>(new Kernel.Infrastructure.Mcp.McpServerRegistry(
            httpClientFactory: null,
            oauthHandler: null,
            logger: null));
        services.AddSingleton<IModelRegistry>(new InMemoryModelRegistry());
        services.AddSingleton<IExperimentTracker>(new InMemoryExperimentTracker());
        services.AddSingleton<InMemorySessionStore>();
        return services;
    }
}
