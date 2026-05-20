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
    [Description("Base URL of the Krnl-AI API (e.g., http://localhost:65335)")]
    public string Endpoint { get; set; } = "http://localhost:65335";

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

    public void ApplyTo(ISettingsService settings)
    {
        settings.Endpoint = Endpoint;
        settings.TimeoutSeconds = TimeoutSeconds;
        settings.MaxRetries = MaxRetries;
        settings.DefaultProvider = DefaultProvider;
        settings.DefaultModel = DefaultModel;
        settings.EnableInlineCompletions = EnableInlineCompletions;
        settings.EnableCodeLens = EnableCodeLens;
        settings.EnableHover = EnableHover;
        settings.EnableCodeActions = EnableCodeActions;
    }

    public void LoadFrom(ISettingsService settings)
    {
        Endpoint = settings.Endpoint;
        TimeoutSeconds = settings.TimeoutSeconds;
        MaxRetries = settings.MaxRetries;
        DefaultProvider = settings.DefaultProvider;
        DefaultModel = settings.DefaultModel;
        EnableInlineCompletions = settings.EnableInlineCompletions;
        EnableCodeLens = settings.EnableCodeLens;
        EnableHover = settings.EnableHover;
        EnableCodeActions = settings.EnableCodeActions;
    }
}
