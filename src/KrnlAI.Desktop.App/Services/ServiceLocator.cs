using System.Net.Http;
using System.Net.Http.Headers;
using KrnlAI.Contracts;
using KrnlAI.Core.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using Refit;
using KrnlAI.Desktop.Infrastructure.KernelClient;
using KrnlAI.Desktop.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KrnlAI.Embedded.Abstractions;
using KrnlAI.Embedded.Models;
using KrnlAI.Embedded.Services;

namespace KrnlAI.Desktop.App.Services;

public class ServiceLocator : IDisposable, IAsyncDisposable
{
    private const string DesktopApiClientName = "DesktopApi";
    private static ServiceLocator? _instance;
    private static readonly object _lock = new();
    public static ServiceLocator Instance
    {
        get
        {
            if (_instance == null) { lock (_lock) { _instance ??= new ServiceLocator(); } }
            return _instance;
        }
    }

    private readonly ServiceProvider _provider;
    private volatile bool _disposed;
    private readonly Lazy<EmbeddedKrnlAI>? _embeddedKernelLazy = new(() =>
        new EmbeddedKrnlAI(CreateLocalEmbeddedKernelOptions()),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public RunMode CurrentMode { get; }
    public IKernelClient KernelClient => Resolve<IKernelClient>()!;
    public IGatewayApi? GatewayApi => Resolve<IGatewayApi>();
    public IAdminApi? AdminApi => Resolve<IAdminApi>();
    public IAudioCapture AudioCapture => Resolve<IAudioCapture>()!;
    public IAudioPlayback AudioPlayback => Resolve<IAudioPlayback>()!;
    public IVideoCapture VideoCapture => Resolve<IVideoCapture>()!;
    public IListeningService ListeningService => Resolve<IListeningService>()!;
    public ISettingsService SettingsService => Resolve<ISettingsService>()!;
    public IThemeService ThemeService => Resolve<IThemeService>()!;
    public ILocalizationService LocalizationService => Resolve<ILocalizationService>()!;
    public ISlashCommandExecutor SlashCommandExecutor => Resolve<ISlashCommandExecutor>()!;
    public ICognitiveStreamProvider CognitiveStreamProvider => Resolve<ICognitiveStreamProvider>()!;
    public IApiKeyManagementService ApiKeyManagementService => Resolve<IApiKeyManagementService>()!;
    public IPeerRankingManagementService PeerRankingManagementService => Resolve<IPeerRankingManagementService>()!;
    public ITelemetryPrivacyService TelemetryPrivacyService => Resolve<ITelemetryPrivacyService>()!;
    public EmbeddedKrnlAI? EmbeddedKernel => _embeddedKernelLazy?.Value;

    public ILogger<T> GetLogger<T>() => _provider!.GetRequiredService<ILogger<T>>();
    public Func<WebRtcService> WebRtcServiceFactory => () => new WebRtcService(GetLogger<WebRtcService>());
    public IThemeService ThemeSvc => _provider.GetRequiredService<IThemeService>();

    /// <summary>Creates a client that always uses the current trusted API endpoint and authentication pipeline.</summary>
    public HttpClient CreateApiClient() => CurrentMode == RunMode.Api
        ? _provider.GetRequiredService<IHttpClientFactory>().CreateClient(DesktopApiClientName)
        : new HttpClient { BaseAddress = new Uri("http://localhost:5235"), Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// Creates the embedded kernel options used by Desktop local AI mode.
    /// </summary>
    public static EmbeddedKernelOptions CreateLocalEmbeddedKernelOptions() => new()
    {
        StoreMode = "Sqlite",
        SqliteMode = "Hybrid",
        VectorMode = "Sqlite",
        CacheMode = "Memory",
        LLmProvider = Environment.GetEnvironmentVariable("KRNL__LLM_PROVIDER") ?? "ollama",
        OllamaEndpoint = Environment.GetEnvironmentVariable("KRNL__OLLAMA_ENDPOINT") ?? "http://localhost:11434"
    };

    private T? Resolve<T>() where T : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceLocator));
        return _provider?.GetService<T>();
    }

    public KanbanService KanbanService => _provider.GetRequiredService<KanbanService>();

    private ServiceLocator()
    {
        try
        {
            var modeEnv = Environment.GetEnvironmentVariable("KRNL__RUN_MODE");
            CurrentMode = string.Equals(modeEnv, "Local", StringComparison.OrdinalIgnoreCase)
                ? RunMode.Local
                : RunMode.Api;

            var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Information));
            var settingsService = new JsonSettingsService();
            var (settings, baseUrl) = ServiceLocatorRegistrations.LoadAppSettings(settingsService);

            var services = new ServiceCollection();
            ServiceLocatorRegistrations.RegisterBaseServices(services, loggerFactory, settingsService);
            ServiceLocatorRegistrations.RegisterByMode(
                services, CurrentMode, loggerFactory, baseUrl, settings, _embeddedKernelLazy?.Value);
ServiceLocatorRegistrations.RegisterSharedServices(services, CurrentMode, loggerFactory, baseUrl);
            _provider = ServiceLocatorRegistrations.BuildProvider(services);

            if (CurrentMode == RunMode.Api
                && (!string.IsNullOrEmpty(settings.AuthToken) || !string.IsNullOrEmpty(settings.RefreshToken)))
            {
                KernelClient.SetTokens(settings.AuthToken, settings.RefreshToken);
            }
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            throw;
        }
    }

    public static void Reset()
    {
        lock (_lock)
        {
            _instance?.Dispose();
            _instance = null;
        }
    }

    private ServiceLocator(IServiceProvider provider)
    {
        CurrentMode = RunMode.Api;
        _provider = provider as ServiceProvider
            ?? throw new ArgumentException($"Expected {nameof(ServiceProvider)}, got {provider.GetType().Name}", nameof(provider));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_embeddedKernelLazy?.IsValueCreated == true)
        {
            try { _embeddedKernelLazy.Value.DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult(); }
            catch (Exception ex) { KrnlLogger.Write(ex); }
        }
        _provider?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_embeddedKernelLazy?.IsValueCreated == true)
            await _embeddedKernelLazy.Value.DisposeAsync().ConfigureAwait(false);
        _provider?.Dispose();
    }
}
