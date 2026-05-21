using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Abstractions.Mcp;
using KrnlAI.Core.Services.ExperimentTracking;
using KrnlAI.Core.Services.Memory;
using KrnlAI.Core.Services.ModelRegistry;
using KrnlAI.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;

namespace KrnlAI.Cli.Services;

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
    public ISchedulerService Scheduler { get; }
    public ISafetyCaseStore SafetyCaseStore { get; }
    public FundamentalRulesEngine RulesEngine { get; }
    public IMcpServerRegistry McpRegistry { get; }
    public IModelRegistry ModelRegistry { get; }
    public IExperimentTracker ExperimentTracker { get; }
    public HttpClient HttpClient { get; }

    public T GetService<T>() where T : notnull => _sp.GetRequiredService<T>();
    private readonly IServiceProvider _sp;

    public CliContext(IServiceProvider sp)
    {
        _sp = sp;
        MomentStore = sp.GetRequiredService<IMomentStore>();
        MomentClassifierStore = sp.GetRequiredService<IMomentClassifierStore>();
        ArchiveStore = sp.GetRequiredService<IArchiveStore>();
        SnapshotService = sp.GetRequiredService<ICognitiveSnapshotService>();
        AnticipationService = sp.GetRequiredService<IAnticipationService>();
        ProspectiveMemory = sp.GetRequiredService<IProspectiveMemoryService>();
        ExecutiveController = sp.GetRequiredService<IExecutiveController>();
        Homeostasis = sp.GetRequiredService<ICognitiveHomeostasis>();
        GoalStore = sp.GetRequiredService<IGoalStore>();
        Scheduler = sp.GetService<ISchedulerService>() ?? new Infrastructure.Scheduling.InMemorySchedulerStore();
        SafetyCaseStore = sp.GetRequiredService<ISafetyCaseStore>();
        RulesEngine = sp.GetRequiredService<FundamentalRulesEngine>();
        McpRegistry = sp.GetRequiredService<IMcpServerRegistry>();
        ModelRegistry = sp.GetRequiredService<IModelRegistry>();
        ExperimentTracker = sp.GetRequiredService<IExperimentTracker>();
        HttpClient = sp.GetService<HttpClient>() ?? new HttpClient();
    }
}
