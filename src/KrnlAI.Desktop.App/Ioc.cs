using System.Net.Http;
using System.Net.Http.Headers;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.Core.Services.Vision;
using KrnlAI.Desktop.Core.Services.Voice;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using KrnlAI.Desktop.Infrastructure.KernelClient;
using KrnlAI.Desktop.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace KrnlAI.Desktop.App;

[System.Obsolete("Use ServiceLocator instead. This class is kept for reference only.")]

public static class Ioc
{
    private static IServiceProvider? _provider;
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var services = new ServiceCollection();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<AuthTokenProvider>();
        services.AddSingleton<IWebRtcService, WebRtcService>();
        services.AddSingleton<ISessionPersistenceService>(_ => new SessionPersistenceService());

        services.AddSingleton<IGatewayApi>(sp =>
        {
            var tokenProvider = sp.GetRequiredService<AuthTokenProvider>();

            var refreshHttpClient = new HttpClient(new DynamicBaseUrlHandler
            {
                InnerHandler = new HttpClientHandler()
            })
            { Timeout = TimeSpan.FromSeconds(30) };

            var handler = new DynamicBaseUrlHandler
            {
                InnerHandler = new AuthTokenHandler(tokenProvider, async ct =>
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
                        return result?.Token;
                    }
                    catch { return null; }
                })
                {
                    InnerHandler = new HttpClientHandler()
                }
            };
            var hc = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
            return RestService.For<IGatewayApi>(hc);
        });

        services.AddSingleton<IKernelClient, KernelClient>();
        services.AddSingleton<ProsodyAnalyzer>();
        services.AddSingleton<FaceExpressionAnalyzer>();
        services.AddSingleton<IKernelAgentClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelSpeechClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<ISnapshotClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IObjectiveClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IInvestigationClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddTransient<ViewModels.MainViewModel>();
        services.AddTransient<ViewModels.ChatViewModel>();
        services.AddTransient<ViewModels.DashboardViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();
        services.AddTransient<ViewModels.MemoryViewModel>();
        services.AddTransient<ViewModels.EpisodesViewModel>();
        services.AddTransient<ViewModels.PoliciesViewModel>();
        services.AddTransient<ViewModels.VideoCallViewModel>();

        _provider = services.BuildServiceProvider();

        var settings = Resolve<ISettingsService>().LoadSettings();
        Resolve<IKernelClient>().SetBaseUrl(settings.ApiBaseUrl);
        if (!string.IsNullOrEmpty(settings.AuthToken))
            Resolve<IKernelClient>().SetAuthToken(settings.AuthToken);
    }

    public static T Resolve<T>() where T : notnull
    {
        if (_provider == null) Initialize();
        return _provider!.GetRequiredService<T>();
    }
}
