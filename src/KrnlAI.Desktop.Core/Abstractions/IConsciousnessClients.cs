using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IInvestigationClient
{
    Task<List<InvestigationInfo>> GetInvestigationsAsync(CancellationToken ct = default);
}

public interface ISnapshotClient
{
    Task<List<SnapshotInfo>> GetSnapshotsAsync(CancellationToken ct = default);
}

public interface IObjectiveClient
{
    Task<List<ObjectiveInfo>> GetObjectivesAsync(CancellationToken ct = default);
    Task<ObjectiveDetail?> GetObjectiveDetailAsync(string id, CancellationToken ct = default);
}
