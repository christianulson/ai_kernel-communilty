using AIKernel.LLMGateway.Core.Abstractions;
using Kernel.Core.Abstractions;
using Kernel.Core.Abstractions.Mcp;
using Kernel.Core.Services.ExperimentTracking;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.ModelRegistry;
using Kernel.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;

namespace AIKernel.Cli.Services;

public sealed class CliContext
{
    public IMomentStore MomentStore { get; }
    public IMomentClassifierStore MomentClassifierStore { get; }
    public IArchiveStore ArchiveStore { get; }
    public ICognitiveSnapshotService SnapshotService { get; }
    public IAnticipationService AnticipationService { get; }
    public IProspectiveMemoryService ProspectiveMemory { get; }
    public IExecutiveController ExecutiveController { get; }
    public ICognitiveHomeostasis Homeostasis { get; }
    public IGoalStore GoalStore { get; }
    public ISafetyCaseStore SafetyCaseStore { get; }
    public FundamentalRulesEngine RulesEngine { get; }
    public IMcpServerRegistry McpRegistry { get; }
    public IModelRegistry ModelRegistry { get; }
    public IExperimentTracker ExperimentTracker { get; }
    public HttpClient HttpClient { get; }

    public CliContext(IServiceProvider sp)
    {
        MomentStore = sp.GetRequiredService<IMomentStore>();
        MomentClassifierStore = sp.GetRequiredService<IMomentClassifierStore>();
        ArchiveStore = sp.GetRequiredService<IArchiveStore>();
        SnapshotService = sp.GetRequiredService<ICognitiveSnapshotService>();
        AnticipationService = sp.GetRequiredService<IAnticipationService>();
        ProspectiveMemory = sp.GetRequiredService<IProspectiveMemoryService>();
        ExecutiveController = sp.GetRequiredService<IExecutiveController>();
        Homeostasis = sp.GetRequiredService<ICognitiveHomeostasis>();
        GoalStore = sp.GetRequiredService<IGoalStore>();
        SafetyCaseStore = sp.GetRequiredService<ISafetyCaseStore>();
        RulesEngine = sp.GetRequiredService<FundamentalRulesEngine>();
        McpRegistry = sp.GetRequiredService<IMcpServerRegistry>();
        ModelRegistry = sp.GetRequiredService<IModelRegistry>();
        ExperimentTracker = sp.GetRequiredService<IExperimentTracker>();
        HttpClient = sp.GetRequiredService<HttpClient>();
    }
}
