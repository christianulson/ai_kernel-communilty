using Kernel.Core.Abstractions;
using Kernel.Core.Services.Anticipation;
using Kernel.Core.Services.Memory;
using Kernel.Core.Services.TemporalDepth;
using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
