using System.Reflection;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Infrastructure.Settings;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class JsonSettingsServiceMigrationTests
{
    [Fact]
    public void NormalizeSettings_UnauthenticatedOldSidecarDefault_ShouldMoveToGatewayProxy()
    {
        var settings = new AppSettings
        {
            ApiBaseUrl = "http://127.0.0.1:5001",
            ApiEndpoint = "http://127.0.0.1:5001",
            AuthToken = null,
            RefreshToken = null
        };

        var normalized = InvokeNormalize(settings);

        Assert.Equal("http://localhost:5235", normalized.ApiBaseUrl);
        Assert.Equal("http://localhost:5235", normalized.ApiEndpoint);
    }

    [Fact]
    public void NormalizeSettings_AuthenticatedSidecarEndpoint_ShouldPreserveExplicitConfiguration()
    {
        var settings = new AppSettings
        {
            ApiBaseUrl = "http://127.0.0.1:5001",
            ApiEndpoint = "http://127.0.0.1:5001",
            AuthToken = "token"
        };

        var normalized = InvokeNormalize(settings);

        Assert.Equal("http://127.0.0.1:5001", normalized.ApiBaseUrl);
        Assert.Equal("http://127.0.0.1:5001", normalized.ApiEndpoint);
    }

    [Fact]
    public void NormalizeSettings_UnauthenticatedOldApiDefault_ShouldMoveToGatewayProxy()
    {
        var settings = new AppSettings
        {
            ApiBaseUrl = "http://localhost:5000",
            ApiEndpoint = "http://localhost:5000",
            AuthToken = null,
            RefreshToken = null
        };

        var normalized = InvokeNormalize(settings);

        Assert.Equal("http://localhost:5235", normalized.ApiBaseUrl);
        Assert.Equal("http://localhost:5235", normalized.ApiEndpoint);
    }

    [Fact]
    public void NormalizeSettings_AuthenticatedOldApiDefault_ShouldMoveToGatewayProxy()
    {
        var settings = new AppSettings
        {
            ApiBaseUrl = "http://localhost:5000",
            ApiEndpoint = "http://localhost:5000",
            AuthToken = "token"
        };

        var normalized = InvokeNormalize(settings);

        Assert.Equal("http://localhost:5235", normalized.ApiBaseUrl);
        Assert.Equal("http://localhost:5235", normalized.ApiEndpoint);
    }

    private static AppSettings InvokeNormalize(AppSettings settings)
    {
        var method = typeof(JsonSettingsService).GetMethod(
            "NormalizeSettings",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        return Assert.IsType<AppSettings>(method!.Invoke(null, [settings]));
    }
}
