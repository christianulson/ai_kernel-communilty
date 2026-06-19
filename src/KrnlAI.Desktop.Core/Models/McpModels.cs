namespace KrnlAI.Desktop.Core.Models;

public sealed record McpServerInfo(
    string ServerId,
    string Name,
    string TransportType,
    bool Enabled,
    bool IsConnected,
    int ToolCount,
    DateTimeOffset? LastUsedAt);

public sealed record McpServerConfig(
    string ServerId,
    string Name,
    string? TransportType,
    string? Command,
    string[]? Args,
    Dictionary<string, string>? Env);
