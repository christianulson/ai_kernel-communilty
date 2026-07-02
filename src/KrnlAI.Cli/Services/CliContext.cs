using KrnlAI.LLMGateway.Core.Abstractions;
using KrnlAI.Core.Abstractions;
using KrnlAI.Core.Abstractions.Mcp;
using KrnlAI.Core.Abstractions.ExperimentTracking;
using KrnlAI.Core.Services.Memory;
using KrnlAI.Core.Abstractions.ModelRegistry;
using KrnlAI.Core.Services.Safety;
using Microsoft.Extensions.DependencyInjection;

namespace KrnlAI.Cli.Services;

public sealed class CliContext(IServiceProvider sp)
{
    public IMomentStore MomentStore { get; } = sp.GetRequiredService<IMomentStore>();
    public IMomentClassifierStore MomentClassifierStore { get; } = sp.GetRequiredService<IMomentClassifierStore>();
    public IArchiveStore ArchiveStore { get; } = sp.GetRequiredService<IArchiveStore>();
    public ICognitiveSnapshotService SnapshotService { get; } = sp.GetRequiredService<ICognitiveSnapshotService>();
    public IAnticipationService AnticipationService { get; } = sp.GetRequiredService<IAnticipationService>();
    public IProspectiveMemoryService ProspectiveMemory { get; } = sp.GetRequiredService<IProspectiveMemoryService>();
    public IExecutiveController ExecutiveController { get; } = sp.GetRequiredService<IExecutiveController>();
    public ICognitiveHomeostasis Homeostasis { get; } = sp.GetRequiredService<ICognitiveHomeostasis>();
    public IGoalStore GoalStore { get; } = sp.GetRequiredService<IGoalStore>();
    public ISchedulerService Scheduler { get; } = sp.GetService<ISchedulerService>() ?? new Infrastructure.Scheduling.InMemorySchedulerStore();
    public ISafetyCaseStore SafetyCaseStore { get; } = sp.GetRequiredService<ISafetyCaseStore>();
    public FundamentalRulesEngine RulesEngine { get; } = sp.GetRequiredService<FundamentalRulesEngine>();
    public IMcpServerRegistry McpRegistry { get; } = sp.GetRequiredService<IMcpServerRegistry>();
    public IModelRegistry ModelRegistry { get; } = sp.GetRequiredService<IModelRegistry>();
    public IExperimentTracker ExperimentTracker { get; } = sp.GetRequiredService<IExperimentTracker>();
    public HttpClient HttpClient { get; } = sp.GetService<HttpClient>() ?? new HttpClient();

    public T GetService<T>() where T : notnull => sp.GetRequiredService<T>();
}
