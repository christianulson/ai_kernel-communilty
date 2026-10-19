using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface ISecurityClient
{
    Task<List<SecurityIncident>> GetSecurityIncidentsAsync(CancellationToken ct = default);
    Task<bool> ResolveSecurityAlertAsync(string alertId, CancellationToken ct = default);
}
