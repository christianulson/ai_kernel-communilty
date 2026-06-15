using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using KrnlAI.Desktop.Infrastructure.KernelClient;
using KrnlAI.Desktop.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace KrnlAI.Desktop.Tests;

public sealed class DesktopDiValidationTests
{
    private sealed class SilentLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private static readonly HashSet<string> SkipTypes =
    [
        "ILogger", "ILogger`1", "ILoggerFactory",
        "IConfiguration", "IHttpClientFactory",
        "AuthTokenProvider", "AuthTokenHandler",
        "IAdminClient", "IAuthClient", "IDashboardClient",
        "IEpisodeClient", "IGoalClient", "IMemoryClient", "IPolicyClient",
        "ISnapshotClient", "IObjectiveClient", "IInvestigationClient",
        "ICognitiveStreamProvider", "ISlashCommandExecutor",
        "IApprovalClient"
    ];

    [Fact]
    public void AllDesktopAbstractions_ShouldBeRegistered()
    {
        var (services, registered) = BuildServiceCollection();

        var coreAbstractions = typeof(IKernelClient).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic && !t.IsNested)
            .Select(t => t.Name)
            .ToHashSet();

        var missing = coreAbstractions
            .Where(name => !SkipTypes.Contains(name) && !registered.Contains(name))
            .OrderBy(n => n)
            .ToArray();

        Assert.True(missing.Length == 0,
            $"Não registradas ({missing.Length}):\n  {string.Join("\n  ", missing)}");
    }

    [Fact]
    public void AllRegisteredServices_ShouldResolve()
    {
        var (services, _) = BuildServiceCollection();
        var provider = services.BuildServiceProvider();
        var failures = new List<string>();

        foreach (var descriptor in services)
        {
            var type = descriptor.ServiceType;
            if (!type.IsInterface) continue;
            if (type.IsGenericType) continue;
            if (SkipTypes.Contains(type.Name)) continue;

            try
            {
                provider.GetRequiredService(type);
            }
            catch (Exception ex)
            {
                failures.Add($"{type.Name}: {ex.GetType().Name} — {ex.Message}");
            }
        }

        Assert.True(failures.Count == 0,
            $"Falhas ({failures.Count}):\n  {string.Join("\n  ", failures)}");
    }

    private static (ServiceCollection services, HashSet<string> registered) BuildServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<AuthTokenProvider>();
        services.AddSingleton<ISettingsService>(new JsonSettingsService());
        services.AddTransient<AuthTokenHandler>();
        services.AddRefitClient<IGatewayApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5000"))
            .AddHttpMessageHandler<AuthTokenHandler>();
        services.AddSingleton<IKernelClient, KernelClient>();
        services.AddSingleton<IKernelAgentClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IKernelSpeechClient>(sp => sp.GetRequiredService<IKernelClient>());
        services.AddSingleton<IAudioCapture>(_ => new AudioCaptureService(new SilentLogger<AudioCaptureService>()));
        services.AddSingleton<IAudioPlayback>(_ => new AudioPlaybackService(new SilentLogger<AudioPlaybackService>()));
        services.AddSingleton<IVideoCapture>(_ => new VideoCaptureService(new SilentLogger<VideoCaptureService>()));
        services.AddSingleton<IListeningService>(sp => new ListeningService(
            sp.GetRequiredService<IAudioCapture>(),
            sp.GetRequiredService<IKernelAgentClient>(),
            sp.GetRequiredService<IKernelSpeechClient>(),
            sp.GetRequiredService<IAudioPlayback>(),
            new SilentLogger<ListeningService>()));
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IOfflineService, OfflineService>();
        services.AddSingleton<IWebRtcService, WebRtcService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IApiKeyManagementService, NullApiKeyManagementService>();
        services.AddSingleton<IPeerRankingManagementService, NullPeerRankingManagementService>();
        services.AddSingleton<ITelemetryPrivacyService, NullTelemetryPrivacyService>();
        services.AddSingleton<ISessionPersistenceService>(_ => new SessionPersistenceService(System.IO.Path.GetTempPath()));

        var registered = services
            .Where(d => d.ServiceType.IsInterface)
            .Select(d => d.ServiceType.Name)
            .ToHashSet();

        return (services, registered);
    }
}
