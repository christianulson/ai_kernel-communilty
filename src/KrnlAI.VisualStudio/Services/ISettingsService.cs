namespace KrnlAI.VisualStudio.Services;

public interface ISettingsService
{
    string Endpoint { get; set; }
    int TimeoutSeconds { get; set; }
    int MaxRetries { get; set; }
    string? DefaultProvider { get; set; }
    string? DefaultModel { get; set; }
    bool EnableInlineCompletions { get; set; }
    bool EnableCodeLens { get; set; }
    bool EnableHover { get; set; }
    bool EnableCodeActions { get; set; }

    void Load();
    void Save();
}
