using Kernel.Core.Abstractions;
using Kernel.Core.Services.Memory;

namespace AIKernel.Cli.Services;

public sealed class CliSeeder(IMomentStore momentStore, IMomentClassifierStore classifierStore)
{
    public async Task SeedAsync()
    {
        var existing = await momentStore.ListRecentAsync(1, CancellationToken.None);
        if (existing.Count > 0)
            return;

        var now = DateTimeOffset.UtcNow;
        var moments = new[]
        {
            new MomentSnapshot("mom-0001", 1, now.AddMinutes(-30), now.AddMinutes(-29),
                null, null, 0.72, 0.85, -0.32, [], [], [], [], new Dictionary<string, string>
                {
                    ["source"] = "seed",
                    ["channel"] = "system"
                }),
            new MomentSnapshot("mom-0002", 2, now.AddMinutes(-20), now.AddMinutes(-19),
                null, null, 0.45, 0.60, 0.10, [], [], [], [], new Dictionary<string, string>
                {
                    ["source"] = "seed",
                    ["channel"] = "memory"
                }),
            new MomentSnapshot("mom-0003", 3, now.AddMinutes(-10), now.AddMinutes(-9),
                null, null, 0.88, 0.95, -0.50, [], [], [], [], new Dictionary<string, string>
                {
                    ["source"] = "seed",
                    ["channel"] = "security"
                }),
        };

        foreach (var m in moments)
            await momentStore.UpsertAsync(m, CancellationToken.None);

        var classifications = new[]
        {
            new MomentClassification("mom-0001", MomentCategory.Routine, 0.95,
                MomentImportance.Zero, MomentNarrativeRole.None, ["system"], new Dictionary<string, string>()),
            new MomentClassification("mom-0002", MomentCategory.Learning, 0.85,
                MomentImportance.Zero, MomentNarrativeRole.None, ["memory"], new Dictionary<string, string>()),
            new MomentClassification("mom-0003", MomentCategory.Anomaly, 0.92,
                MomentImportance.Zero, MomentNarrativeRole.None, ["security"], new Dictionary<string, string>()),
        };

        foreach (var c in classifications)
            await classifierStore.StoreAsync(c, CancellationToken.None);
    }
}
