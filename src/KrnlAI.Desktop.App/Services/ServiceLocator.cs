using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using Refit;
using KrnlAI.Desktop.Infrastructure.KernelClient;
using KrnlAI.Desktop.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.Services;

public class ServiceLocator : IDisposable
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

    public IKernelClient KernelClient => _provider.GetRequiredService<IKernelClient>();
    public IAudioCapture AudioCapture => _provider.GetRequiredService<IAudioCapture>();
    public IAudioPlayback AudioPlayback => _provider.GetRequiredService<IAudioPlayback>();
    public IVideoCapture VideoCapture => _provider.GetRequiredService<IVideoCapture>();
    public IListeningService ListeningService => _provider.GetRequiredService<IListeningService>();
    public ISettingsService SettingsService => _provider.GetRequiredService<ISettingsService>();
    public IThemeService ThemeService => _provider.GetRequiredService<IThemeService>();
    public ILocalizationService LocalizationService => _provider.GetRequiredService<ILocalizationService>();

    public ILogger<T> GetLogger<T>() => _provider.GetRequiredService<ILogger<T>>();
    public Func<WebRtcService> WebRtcServiceFactory => () => new WebRtcService(GetLogger<WebRtcService>());

    private ServiceLocator()
    {
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        var settingsService = new JsonSettingsService();
        var settings = settingsService.LoadSettings();
        var baseUrl = settings.ApiEndpoint ?? settings.ApiBaseUrl ?? "http://localhost:5000";

        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton<AuthTokenProvider>();
        services.AddSingleton<ISettingsService>(settingsService);
        services.AddTransient<AuthTokenHandler>();
        services.AddSingleton<DynamicBaseUrlHandler>();
        // Initialize the dynamic base URL handler with the configured URL
        DynamicBaseUrlHandler.SetBaseUrl(baseUrl);
        services.AddRefitClient<IGatewayApi>()
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://localhost"); c.Timeout = TimeSpan.FromSeconds(60); })
            .AddHttpMessageHandler<DynamicBaseUrlHandler>()
            .AddHttpMessageHandler<AuthTokenHandler>();
        services.AddSingleton<IKernelClient, KernelClient>();
        services.AddSingleton<IKernelAgentClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelSpeechClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IAudioCapture>(_ => new AudioCaptureService(loggerFactory.CreateLogger<AudioCaptureService>()));
        services.AddSingleton<IAudioPlayback>(_ => new AudioPlaybackService(loggerFactory.CreateLogger<AudioPlaybackService>()));
        services.AddSingleton<IVideoCapture>(_ => new VideoCaptureService(loggerFactory.CreateLogger<VideoCaptureService>()));
        services.AddSingleton<IListeningService>(sp => new ListeningService(
            sp.GetRequiredService<IAudioCapture>(),
            sp.GetRequiredService<IKernelAgentClient>(),
            sp.GetRequiredService<IKernelSpeechClient>(),
            sp.GetRequiredService<IAudioPlayback>(),
            sp.GetRequiredService<ILogger<ListeningService>>()));
        services.AddSingleton<IThemeService, ThemeService>();
        var localizationService = new LocalizationService();
        services.AddSingleton<ILocalizationService>(localizationService);
        ServiceLocatorAccess.SetLocalizationService(localizationService);
        services.AddSingleton<IOfflineService, OfflineService>();
        services.AddSingleton<ThemeManager>();

        _provider = services.BuildServiceProvider();

        // Initialize ThemeManager so it hooks theme change events
        _provider.GetRequiredService<ThemeManager>();

        if (!string.IsNullOrEmpty(settings.AuthToken))
            KernelClient.SetAuthToken(settings.AuthToken);
    }

    public void Dispose()
    {
        _provider?.Dispose();
    }
}
