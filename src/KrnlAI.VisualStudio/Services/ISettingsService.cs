namespace KrnlAI.VisualStudio.Services;

public interface ISettingsService
{
    string Endpoint { get; set; }
    int TimeoutSeconds { get; set; }
    int MaxRetries { get; set; }
    string? DefaultProvider { get; set; }
    string? DefaultModel { get; set; }

    void Load();
    void Save();
}
