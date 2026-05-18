using System.Net.Http;
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

        services.AddSingleton<IGatewayApi>(sp =>
        {
            var handler = new DynamicBaseUrlHandler
            {
                InnerHandler = new AuthTokenHandler(sp.GetRequiredService<AuthTokenProvider>())
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
