using System.ComponentModel;
using System.Runtime.InteropServices;
using KrnlAI.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.Options;

[Guid("C3D4E5F6-A7B8-9012-CDEF-123456789012")]
public sealed class KrnlAIOptionsPage : DialogPage
{
    [Category("Krnl-AI")]
    [DisplayName("API Endpoint")]
    [Description("Base URL of the Krnl-AI API (e.g., http://localhost:5235)")]
    public string Endpoint { get; set; } = "http://localhost:5235";

    [Category("Krnl-AI")]
    [DisplayName("Runtime Mode")]
    [Description("Embedded uses local Sidecar, LocalApi requires loopback, RemoteApi allows remote API endpoints")]
    public Services.KernelRuntimeMode RuntimeMode { get; set; } = KernelRuntimeMode.LocalApi;

    [Category("Krnl-AI")]
    [DisplayName("Sidecar Port")]
    [Description("Port used by Embedded mode through the local Sidecar")]
    public int SidecarPort { get; set; } = 5001;

    [Category("Krnl-AI")]
    [DisplayName("Timeout (seconds)")]
    [Description("Request timeout in seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [Category("Krnl-AI")]
    [DisplayName("Max Retries")]
    [Description("Number of connection retries before failing")]
    public int MaxRetries { get; set; } = 3;

    [Category("Krnl-AI")]
    [DisplayName("Default Provider")]
    [Description("Default LLM provider (e.g., openai, anthropic)")]
    public string? DefaultProvider { get; set; }

    [Category("Krnl-AI")]
    [DisplayName("Default Model")]
    [Description("Default model name (e.g., gpt-4o, claude-sonnet-4-5)")]
    public string? DefaultModel { get; set; }

    [Category("Krnl-AI")]
    [DisplayName("Auto-connect on start")]
    [Description("Automatically connect to the API endpoint when Visual Studio starts")]
    public bool AutoConnect { get; set; } = true;

    [Category("Krnl-AI - Editor Intelligence")]
    [DisplayName("Enable Inline Completions")]
    [Description("Show AI-powered inline code completions while typing")]
    public bool EnableInlineCompletions { get; set; } = true;

    [Category("Krnl-AI - Editor Intelligence")]
    [DisplayName("Enable CodeLens")]
    [Description("Show cognitive complexity metrics in CodeLens annotations")]
    public bool EnableCodeLens { get; set; } = true;

    [Category("Krnl-AI - Editor Intelligence")]
    [DisplayName("Enable Hover")]
    [Description("Show AI explanation when hovering over code")]
    public bool EnableHover { get; set; } = true;

    [Category("Krnl-AI - Editor Intelligence")]
    [DisplayName("Enable Code Actions")]
    [Description("Show AI-powered suggestions in the lightbulb menu")]
    public bool EnableCodeActions { get; set; } = true;

    [Category("Krnl-AI - Chat")]
    [DisplayName("Approval Mode")]
    [Description("Chat-only: no actions, Confirm: approve per action, Full-approval: approve all")]
    public Services.ApprovalMode ApprovalMode { get; set; } = ApprovalMode.Confirm;

    [Category("Krnl-AI - Chat")]
    [DisplayName("Enable Artifact Rendering")]
    [Description("Render Markdown, tables, charts, and Mermaid diagrams in chat")]
    public bool EnableArtifactRendering { get; set; } = true;

    [Category("Krnl-AI - Chat")]
    [DisplayName("Enable Streaming")]
    [Description("Stream agent responses in real-time via SignalR")]
    public bool EnableStreaming { get; set; } = true;

    [Category("Krnl-AI - Cloud")]
    [DisplayName("Cloud Mode")]
    [Description("Auto: decide based on latency, AlwaysCloud: always use cloud, AlwaysLocal: never use cloud")]
    public Services.CloudMode CloudMode { get; set; } = CloudMode.Auto;

    [Category("Krnl-AI - Cloud")]
    [DisplayName("Cloud Endpoint")]
    [Description("Optional cloud API endpoint for offloading heavy computations")]
    public string? CloudEndpoint { get; set; }

    [Category("Krnl-AI - Privacy")]
    [DisplayName("Enable Usage Tracking")]
    [Description("Track usage statistics locally for dashboard display")]
    public bool EnableUsageTracking { get; set; } = true;

    public void ApplyTo(ISettingsService settings)
    {
        settings.Endpoint = Endpoint;
        settings.RuntimeMode = RuntimeMode;
        settings.SidecarPort = SidecarPort;
        settings.TimeoutSeconds = TimeoutSeconds;
        settings.MaxRetries = MaxRetries;
        settings.DefaultProvider = DefaultProvider;
        settings.DefaultModel = DefaultModel;
        settings.EnableInlineCompletions = EnableInlineCompletions;
        settings.EnableCodeLens = EnableCodeLens;
        settings.EnableHover = EnableHover;
        settings.EnableCodeActions = EnableCodeActions;
        settings.ApprovalMode = ApprovalMode;
        settings.EnableArtifactRendering = EnableArtifactRendering;
        settings.EnableStreaming = EnableStreaming;
        settings.CloudMode = CloudMode;
        settings.CloudEndpoint = CloudEndpoint;
        settings.EnableUsageTracking = EnableUsageTracking;
    }

    public void LoadFrom(ISettingsService settings)
    {
        Endpoint = settings.Endpoint;
        RuntimeMode = settings.RuntimeMode;
        SidecarPort = settings.SidecarPort;
        TimeoutSeconds = settings.TimeoutSeconds;
        MaxRetries = settings.MaxRetries;
        DefaultProvider = settings.DefaultProvider;
        DefaultModel = settings.DefaultModel;
        EnableInlineCompletions = settings.EnableInlineCompletions;
        EnableCodeLens = settings.EnableCodeLens;
        EnableHover = settings.EnableHover;
        EnableCodeActions = settings.EnableCodeActions;
        ApprovalMode = settings.ApprovalMode;
        EnableArtifactRendering = settings.EnableArtifactRendering;
        EnableStreaming = settings.EnableStreaming;
        CloudMode = settings.CloudMode;
        CloudEndpoint = settings.CloudEndpoint;
        EnableUsageTracking = settings.EnableUsageTracking;
    }
}
