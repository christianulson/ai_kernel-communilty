namespace KrnlAI.Desktop.Core.Models;

public sealed record UserServiceInfo(
    string ServiceType,
    bool Configured,
    bool Enabled,
    DateTimeOffset? LastUsedAt);

public sealed record UserServiceUpdateRequest(
    Dictionary<string, string> Credentials,
    bool? Enabled = null);
