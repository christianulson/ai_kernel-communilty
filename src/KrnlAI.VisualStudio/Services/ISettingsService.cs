namespace KrnlAI.VisualStudio.Services;

public enum ApprovalMode { ChatOnly, Confirm, FullApproval }
public enum KernelRuntimeMode { Embedded, LocalApi, RemoteApi }

public interface ISettingsService
{
    string Endpoint { get; set; }
    KernelRuntimeMode RuntimeMode { get; set; }
    int SidecarPort { get; set; }
    int TimeoutSeconds { get; set; }
    int MaxRetries { get; set; }
    string? DefaultProvider { get; set; }
    string? DefaultModel { get; set; }
    bool EnableInlineCompletions { get; set; }
    bool EnableCodeLens { get; set; }
    bool EnableHover { get; set; }
    bool EnableCodeActions { get; set; }
    ApprovalMode ApprovalMode { get; set; }
    bool EnableArtifactRendering { get; set; }
    bool EnableStreaming { get; set; }
    CloudMode CloudMode { get; set; }
    string? CloudEndpoint { get; set; }
    bool EnableUsageTracking { get; set; }

    void Load();
    void Save();
}
