using Kernel.Core.Abstractions;
using Kernel.Core.Services.Anticipation;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.TemporalDepth;
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
    }
}
