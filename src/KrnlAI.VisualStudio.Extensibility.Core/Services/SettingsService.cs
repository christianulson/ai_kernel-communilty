using KrnlAI.VisualStudio.Extensibility.Core.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.VisualStudio.Extensibility.Core.Services;

/// <summary>Approval mode for agent actions.</summary>
public enum ApprovalMode
{
    /// <summary>Always confirm.</summary>
    Confirm = 0,

    /// <summary>Never confirm.</summary>
    Never = 1
}

/// <summary>Cloud delegation mode.</summary>
public enum CloudMode
{
    /// <summary>Automatic decision.</summary>
    Auto = 0,

    /// <summary>Force local.</summary>
    Local = 1,

    /// <summary>Force cloud.</summary>
    Cloud = 2
}

/// <summary>Extension settings (portable; persistence via ISettingsStore).</summary>
public interface ISettingsService
{
    /// <summary>Kernel endpoint.</summary>
    string Endpoint { get; set; }

    /// <summary>Runtime mode.</summary>
    KernelRuntimeMode RuntimeMode { get; set; }

    /// <summary>Sidecar port (embedded mode).</summary>
    int SidecarPort { get; set; }

    /// <summary>Enables streaming responses.</summary>
    bool EnableStreaming { get; set; }

    /// <summary>Loads settings from the store.</summary>
    void Load();

    /// <summary>Saves settings to the store.</summary>
    void Save();
}

/// <summary>
/// Settings service backed by an <see cref="ISettingsStore"/> (portable).
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private const string CollectionPath = "KrnlAI";
    private readonly ISettingsStore _store;
    private readonly ILogger<SettingsService> _logger;

    /// <summary>Creates a new instance.</summary>
    public SettingsService(ISettingsStore store, ILogger<SettingsService>? logger = null)
    {
        _store = store;
        _logger = logger ?? NullLogger<SettingsService>.Instance;
    }

    /// <inheritdoc/>
    public string Endpoint { get; set; } = "http://localhost:5235";

    /// <inheritdoc/>
    public KernelRuntimeMode RuntimeMode { get; set; } = KernelRuntimeMode.LocalApi;

    /// <inheritdoc/>
    public int SidecarPort { get; set; } = 5001;

    /// <inheritdoc/>
    public bool EnableStreaming { get; set; } = true;

    /// <summary>Timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Max retries.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Default provider.</summary>
    public string? DefaultProvider { get; set; }

    /// <summary>Default model.</summary>
    public string? DefaultModel { get; set; }

    /// <summary>Approval mode.</summary>
    public ApprovalMode ApprovalMode { get; set; } = ApprovalMode.Confirm;

    /// <summary>Cloud mode.</summary>
    public CloudMode CloudMode { get; set; } = CloudMode.Auto;

    /// <summary>Cloud endpoint.</summary>
    public string? CloudEndpoint { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        try
        {
            if (!_store.CollectionExists(CollectionPath))
                return;

            Endpoint = ReadString("Endpoint", "http://localhost:5235");
            RuntimeMode = (KernelRuntimeMode)ReadInt("RuntimeMode", (int)KernelRuntimeMode.LocalApi);
            SidecarPort = ReadInt("SidecarPort", 5001);
            TimeoutSeconds = ReadInt("TimeoutSeconds", 30);
            MaxRetries = ReadInt("MaxRetries", 3);
            DefaultProvider = ReadString("DefaultProvider", null);
            DefaultModel = ReadString("DefaultModel", null);
            ApprovalMode = (ApprovalMode)ReadInt("ApprovalMode", (int)ApprovalMode.Confirm);
            CloudMode = (CloudMode)ReadInt("CloudMode", (int)CloudMode.Auto);
            CloudEndpoint = ReadString("CloudEndpoint", null);
            EnableStreaming = ReadBool("EnableStreaming", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
        }
    }

    /// <inheritdoc/>
    public void Save()
    {
        try
        {
            if (!_store.CollectionExists(CollectionPath))
                _store.CreateCollection(CollectionPath);

            _store.SetString(CollectionPath, "Endpoint", Endpoint);
            _store.SetInt32(CollectionPath, "RuntimeMode", (int)RuntimeMode);
            _store.SetInt32(CollectionPath, "SidecarPort", SidecarPort);
            _store.SetInt32(CollectionPath, "TimeoutSeconds", TimeoutSeconds);
            _store.SetInt32(CollectionPath, "MaxRetries", MaxRetries);
            _store.SetString(CollectionPath, "DefaultProvider", DefaultProvider ?? string.Empty);
            _store.SetString(CollectionPath, "DefaultModel", DefaultModel ?? string.Empty);
            _store.SetInt32(CollectionPath, "ApprovalMode", (int)ApprovalMode);
            _store.SetInt32(CollectionPath, "CloudMode", (int)CloudMode);
            _store.SetString(CollectionPath, "CloudEndpoint", CloudEndpoint ?? string.Empty);
            _store.SetString(CollectionPath, "EnableStreaming", EnableStreaming ? "1" : "0");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    private string ReadString(string key, string? defaultValue)
        => _store.GetString(CollectionPath, key, defaultValue ?? string.Empty);

    private int ReadInt(string key, int defaultValue)
        => _store.GetInt32(CollectionPath, key, defaultValue);

    private bool ReadBool(string key, bool defaultValue)
        => _store.GetString(CollectionPath, key, defaultValue ? "1" : "0") == "1";
}