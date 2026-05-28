using System.Net.Http;
using System.Net.Http.Headers;
using KrnlAI.Core.Model;
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
using KrnlAI.Embedded;
using KrnlAI.Embedded.Abstractions;

namespace KrnlAI.Desktop.App.Services;

public class ServiceLocator : IDisposable, IAsyncDisposable
{
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
    private Lazy<EmbeddedKrnlAI>? _embeddedKernelLazy = new(() =>
        new EmbeddedKrnlAI(new EmbeddedKernelOptions
        {
            StoreMode = "InMemory",
            VectorMode = "InMemory",
            CacheMode = "Memory",
            LLmProvider = Environment.GetEnvironmentVariable("KRNL__LLM_PROVIDER") ?? "ollama",
            OllamaEndpoint = Environment.GetEnvironmentVariable("KRNL__OLLAMA_ENDPOINT") ?? "http://localhost:11434"
        }),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public RunMode CurrentMode { get; }
    public IKernelClient KernelClient => Resolve<IKernelClient>()!;
    public IGatewayApi? GatewayApi => Resolve<IGatewayApi>();
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
    public ITelemetryPrivacyService TelemetryPrivacyService => Resolve<ITelemetryPrivacyService>()!;
    public EmbeddedKrnlAI? EmbeddedKernel => _embeddedKernelLazy?.Value;

    public ILogger<T> GetLogger<T>() => _provider!.GetRequiredService<ILogger<T>>();
    public Func<WebRtcService> WebRtcServiceFactory => () => new WebRtcService(GetLogger<WebRtcService>());
    public IThemeService ThemeSvc => _provider.GetRequiredService<IThemeService>();

    private T? Resolve<T>() where T : class =>
        _disposed ? null : _provider?.GetService<T>();

    public KanbanService KanbanService => _provider.GetRequiredService<KanbanService>();

    private ServiceLocator()
    {
        try
        {
            var modeEnv = Environment.GetEnvironmentVariable("KRNL__RUN_MODE");
            CurrentMode = string.Equals(modeEnv, "Local", StringComparison.OrdinalIgnoreCase)
                ? RunMode.Local
                : RunMode.Api;

            var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));

            var settingsService = new JsonSettingsService();
            var settings = settingsService.LoadSettings();
            var baseUrl = Environment.GetEnvironmentVariable("KRNL__API_BASE_URL")
                           ?? settings.ApiEndpoint
                           ?? settings.ApiBaseUrl
                           ?? "http://localhost:5235";

            var services = new ServiceCollection();
            services.AddSingleton(loggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton<ISettingsService>(settingsService);
            services.AddSingleton<ISessionPersistenceService>(_ => new SessionPersistenceService());

        if (CurrentMode == RunMode.Local)
        {
            RegisterLocalMode(services, loggerFactory);
        }
        else
            {
            RegisterApiMode(services, loggerFactory, baseUrl, settings);
        }

            services.AddSingleton<IAudioCapture>(_ => new AudioCaptureService(loggerFactory.CreateLogger<AudioCaptureService>()));
            services.AddSingleton<IAudioPlayback>(_ => new AudioPlaybackService(loggerFactory.CreateLogger<AudioPlaybackService>()));
            services.AddSingleton<IVideoCapture>(_ => new VideoCaptureService(loggerFactory.CreateLogger<VideoCaptureService>()));
            var isLocal = CurrentMode == RunMode.Local;
            services.AddSingleton<IListeningService>(sp => new ListeningService(
                sp.GetRequiredService<IAudioCapture>(),
                sp.GetRequiredService<IKernelAgentClient>(),
                sp.GetRequiredService<IKernelSpeechClient>(),
                sp.GetRequiredService<IAudioPlayback>(),
                sp.GetRequiredService<ILogger<ListeningService>>(),
                isLocalMode: isLocal));
            services.AddSingleton<IThemeService, ThemeService>();
            var localizationService = new LocalizationService();
            services.AddSingleton<ILocalizationService>(localizationService);
            ServiceLocatorAccess.SetLocalizationService(localizationService);
            services.AddSingleton<IOfflineService, OfflineService>();
            services.AddSingleton<ThemeManager>();
            services.AddSingleton(new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) });
            services.AddSingleton<KanbanService>();

            _provider = services.BuildServiceProvider();

            _provider.GetRequiredService<ThemeManager>();

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

    private void RegisterLocalMode(ServiceCollection services, ILoggerFactory loggerFactory)
    {
        var kernel = EmbeddedKernel ?? throw new InvalidOperationException("Embedded kernel is not available.");
        services.AddSingleton<IEmbeddedKrnlAI>(kernel);
        services.AddSingleton<IKernelClient, EmbeddedKernelClient>();
        services.AddSingleton<IKernelAgentClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelSpeechClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IApiKeyManagementService, NullApiKeyManagementService>();
        services.AddSingleton<ITelemetryPrivacyService, NullTelemetryPrivacyService>();

        services.AddSingleton<ISlashCommandExecutor>(
            sp => new EmbeddedSlashCommandExecutor(sp.GetRequiredService<IEmbeddedKrnlAI>()));

        var cognitiveStreamer = new CognitiveStreamer(
            loggerFactory.CreateLogger<CognitiveStreamer>(),
            new CognitiveStreamConfig());
        services.AddSingleton<ICognitiveStreamProvider>(
            _ => new EmbeddedCognitiveStreamProvider(cognitiveStreamer));
    }

    private void RegisterApiMode(ServiceCollection services, ILoggerFactory loggerFactory, string baseUrl, AppSettings settings)
    {
        services.AddSingleton<AuthTokenProvider>();
        services.AddSingleton<DynamicBaseUrlHandler>();
        DynamicBaseUrlHandler.SetBaseUrl(baseUrl);

        var refreshHttpClient = new HttpClient(new DynamicBaseUrlHandler
        {
            InnerHandler = new HttpClientHandler()
        })
        { Timeout = TimeSpan.FromSeconds(30) };

        services.AddTransient<AuthTokenHandler>(sp =>
        {
            var tokenProvider = sp.GetRequiredService<AuthTokenProvider>();
            return new AuthTokenHandler(tokenProvider, async ct =>
            {
                try
                {
                    var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
                    if (tokenProvider.RefreshToken != null)
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(
                            new RefreshTokenRequest(tokenProvider.RefreshToken));
                        refreshRequest.Content = new StringContent(json,
                            System.Text.Encoding.UTF8, "application/json");
                    }
                    if (!string.IsNullOrEmpty(tokenProvider.Token))
                        refreshRequest.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", tokenProvider.Token);

                    var refreshResponse = await refreshHttpClient.SendAsync(refreshRequest, ct);
                    if (!refreshResponse.IsSuccessStatusCode) return null;

                    var body = await refreshResponse.Content.ReadAsStringAsync(ct);
                    var result = System.Text.Json.JsonSerializer
                        .Deserialize<RefreshTokenResponseDto>(body);
                    if (!string.IsNullOrEmpty(result?.RefreshToken))
                        tokenProvider.RefreshToken = result.RefreshToken;
                    return result?.Token;
                }
                catch { return null; }
            });
        });

        services.AddRefitClient<IGatewayApi>()
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://localhost"); c.Timeout = TimeSpan.FromSeconds(60); })
            .AddHttpMessageHandler<DynamicBaseUrlHandler>()
            .AddHttpMessageHandler<AuthTokenHandler>();
        services.AddSingleton<IKernelClient, KernelClient>();
        services.AddSingleton<IKernelAgentClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelSpeechClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IApiKeyManagementService>(sp =>
            new HttpApiKeyManagementService(new HttpClient(new AuthTokenHandler(sp.GetRequiredService<AuthTokenProvider>()))
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            }));
        services.AddSingleton<ITelemetryPrivacyService>(sp =>
            new HttpTelemetryPrivacyService(new HttpClient(new AuthTokenHandler(sp.GetRequiredService<AuthTokenProvider>()))
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            }));

        services.AddSingleton<ISlashCommandExecutor>(
            _ => new HttpSlashCommandExecutor(baseUrl));
        services.AddSingleton<ICognitiveStreamProvider>(
            _ => new HttpCognitiveStreamProvider(baseUrl));
    }

    public static void ConfigureForTests(IServiceProvider provider)
    {
        lock (_lock)
        {
            _instance?.Dispose();
            _instance = new ServiceLocator(provider);
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
        _provider = (ServiceProvider)provider;
    }

    public void Dispose()
    {
        if (_disposed) return;
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_embeddedKernelLazy?.IsValueCreated == true)
            await _embeddedKernelLazy.Value.DisposeAsync().ConfigureAwait(false);
        _provider?.Dispose();
        _disposed = true;
    }
}
