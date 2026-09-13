using System.Net.Http;
using System.Net.Http.Headers;
using KrnlAI.Contracts;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using KrnlAI.Desktop.Infrastructure.KernelClient;
using KrnlAI.Desktop.Infrastructure.Settings;
using KrnlAI.Embedded.Abstractions;
using KrnlAI.Embedded.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace KrnlAI.Desktop.App.Services;

/// <summary>
/// Container registration groups for <see cref="ServiceLocator"/>.
/// Keeps the locator focused on resolution while registrations live per area.
/// </summary>
internal static class ServiceLocatorRegistrations
{
    internal const string DesktopApiClientName = "DesktopApi";

    internal static (AppSettings Settings, string BaseUrl) LoadAppSettings(JsonSettingsService settingsService)
    {
        var settings = settingsService.LoadSettings();
        var baseUrl = Environment.GetEnvironmentVariable("KRNL__API_BASE_URL")
                       ?? settings.ApiEndpoint
                       ?? settings.ApiBaseUrl
                       ?? "http://localhost:5235";
        return (settings, baseUrl);
    }

    internal static void RegisterBaseServices(ServiceCollection services, ILoggerFactory loggerFactory, ISettingsService settingsService)
    {
        services.AddSingleton(loggerFactory);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton<ISettingsService>(settingsService);
        services.AddSingleton<ISessionPersistenceService>(_ => new SessionPersistenceService());
    }

    internal static void RegisterByMode(
        ServiceCollection services,
        RunMode mode,
        ILoggerFactory loggerFactory,
        string baseUrl,
        AppSettings settings,
        EmbeddedKrnlAI? embeddedKernel)
    {
        if (mode == RunMode.Local)
        {
            RegisterLocalMode(services, loggerFactory, embeddedKernel);
        }
        else
        {
            RegisterApiMode(services, loggerFactory, baseUrl, settings);
        }
    }

    internal static void RegisterSharedServices(ServiceCollection services, RunMode mode, ILoggerFactory loggerFactory, string baseUrl)
    {
        services.AddSingleton<IAudioCapture>(_ => new AudioCaptureService(loggerFactory.CreateLogger<AudioCaptureService>()));
        services.AddSingleton<IAudioPlayback>(_ => new AudioPlaybackService(loggerFactory.CreateLogger<AudioPlaybackService>()));
        services.AddSingleton<IVideoCapture>(_ => new VideoCaptureService(loggerFactory.CreateLogger<VideoCaptureService>()));
        var isLocal = mode == RunMode.Local;
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
        services.AddSingleton<ThemeManager>();
        services.AddSingleton(new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) });
        services.AddSingleton<KanbanService>();
    }

    internal static ServiceProvider BuildProvider(ServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ThemeManager>();
        return provider;
    }

    private static void RegisterLocalMode(ServiceCollection services, ILoggerFactory loggerFactory, EmbeddedKrnlAI? embeddedKernel)
    {
        var kernel = embeddedKernel ?? throw new InvalidOperationException("Embedded kernel is not available.");
        services.AddSingleton<IEmbeddedKrnlAI>(kernel);
        services.AddSingleton<IKernelClient, EmbeddedKernelClient>();
        services.AddSingleton<IBackendApi>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelAgentClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelSpeechClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IApiKeyManagementService, NullApiKeyManagementService>();
        services.AddSingleton<IPeerRankingManagementService, NullPeerRankingManagementService>();
        services.AddSingleton<ITelemetryPrivacyService, NullTelemetryPrivacyService>();

        services.AddSingleton<ISlashCommandExecutor>(
            sp => new EmbeddedSlashCommandExecutor(sp.GetRequiredService<IEmbeddedKrnlAI>()));

        var cognitiveStreamer = kernel.CognitiveStreamer ?? new KrnlAI.Cognition.Services.CognitiveStreamer(
            loggerFactory.CreateLogger<KrnlAI.Cognition.Services.CognitiveStreamer>(),
            new CognitiveStreamConfig());
        services.AddSingleton<ICognitiveStreamProvider>(
            _ => new EmbeddedCognitiveStreamProvider(cognitiveStreamer));
        services.AddSingleton<IAdminApi, NullAdminApi>();
    }

    private static void RegisterApiMode(ServiceCollection services, ILoggerFactory loggerFactory, string baseUrl, AppSettings settings)
    {
        services.AddSingleton<AuthTokenProvider>();
        services.AddTransient<DynamicBaseUrlHandler>();
        DynamicBaseUrlHandler.SetBaseUrl(baseUrl);

        var refreshHttpClient = new HttpClient(new DynamicBaseUrlHandler
        {
            InnerHandler = new HttpClientHandler()
        })
        { Timeout = TimeSpan.FromSeconds(30) };

        RegisterAuthHandlers(services, refreshHttpClient);

        services.AddHttpClient(DesktopApiClientName, c =>
            {
                c.BaseAddress = new Uri("http://localhost");
                c.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<DynamicBaseUrlHandler>()
            .AddHttpMessageHandler<AuthTokenHandler>();

        services.AddRefitClient<IGatewayApi>()
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://localhost"); c.Timeout = TimeSpan.FromSeconds(60); })
            .AddHttpMessageHandler<DynamicBaseUrlHandler>()
            .AddHttpMessageHandler<AuthTokenHandler>();
        services.AddSingleton<IKernelClient, KernelClient>();
        services.AddSingleton<IBackendApi>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelAgentClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelSpeechClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IApiKeyManagementService>(sp =>
            new HttpApiKeyManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient(DesktopApiClientName)));
        services.AddSingleton<IPeerRankingManagementService>(sp =>
            new HttpPeerRankingManagementService(sp.GetRequiredService<IHttpClientFactory>().CreateClient(DesktopApiClientName)));
        services.AddSingleton<ITelemetryPrivacyService>(sp =>
            new HttpTelemetryPrivacyService(sp.GetRequiredService<IHttpClientFactory>().CreateClient(DesktopApiClientName)));

        services.AddSingleton<ISlashCommandExecutor>(
            sp => new HttpSlashCommandExecutor(sp.GetRequiredService<IHttpClientFactory>().CreateClient(DesktopApiClientName)));
        services.AddSingleton<ICognitiveStreamProvider>(
            sp => new HttpCognitiveStreamProvider(sp.GetRequiredService<IHttpClientFactory>().CreateClient(DesktopApiClientName)));

        services.AddRefitClient<IAdminApi>()
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://localhost"); c.Timeout = TimeSpan.FromSeconds(30); })
            .AddHttpMessageHandler<DynamicBaseUrlHandler>()
            .AddHttpMessageHandler<AuthTokenHandler>();
    }

    private static void RegisterAuthHandlers(ServiceCollection services, HttpClient refreshHttpClient)
    {
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

                    var refreshResponse = await refreshHttpClient.SendAsync(refreshRequest, ct).ConfigureAwait(false);
                    if (!refreshResponse.IsSuccessStatusCode) return null;

                    var body = await refreshResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    var result = System.Text.Json.JsonSerializer
                        .Deserialize<RefreshTokenResponseDto>(body);
                    if (!string.IsNullOrEmpty(result?.RefreshToken))
                        tokenProvider.RefreshToken = result.RefreshToken;
                    return result?.Token;
                }
                catch { return null; }
            });
        });
    }
}