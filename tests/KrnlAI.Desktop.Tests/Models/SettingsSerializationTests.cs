using System.Text.Json;
using System.Text.Json.Serialization;

namespace KrnlAI.Desktop.Tests.Models;

public class SettingsSerializationTests
{
    private static readonly JsonSerializerOptions SafeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private static readonly JsonSerializerOptions UnsafeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void SerializeSettings_WithNan_ShouldNotThrow()
    {
        var settings = new AppSettings
        {
            VoiceDetectionThreshold = float.NaN,
            SilenceDurationMs = 1500,
            SpeakerVolume = float.PositiveInfinity
        };
        var ex = Record.Exception(() => JsonSerializer.Serialize(settings, SafeOptions));
        Assert.Null(ex);
    }

    [Fact]
    public void SerializeSettings_WithNan_WithoutAllowNamedFloatingPoint_ShouldThrow()
    {
        var settings = new AppSettings
        {
            VoiceDetectionThreshold = float.NaN
        };
        Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize(settings, UnsafeOptions));
    }

    [Fact]
    public void SerializeSettings_WithNormalValues_ShouldRoundTrip()
    {
        var original = new AppSettings
        {
            ApiBaseUrl = "http://localhost:5235",
            AuthToken = "test-token",
            Username = "admin-test",
            VoiceDetectionThreshold = 0.01f,
            SilenceDurationMs = 1500,
            SpeakerVolume = 1.0f
        };
        var json = JsonSerializer.Serialize(original, SafeOptions);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json, SafeOptions);
        Assert.NotNull(deserialized);
        Assert.Equal(original.ApiBaseUrl, deserialized.ApiBaseUrl);
        Assert.Equal(original.AuthToken, deserialized.AuthToken);
        Assert.Equal(original.Username, deserialized.Username);
        Assert.Equal(original.VoiceDetectionThreshold, deserialized.VoiceDetectionThreshold);
        Assert.Equal(original.SilenceDurationMs, deserialized.SilenceDurationMs);
        Assert.Equal(original.SpeakerVolume, deserialized.SpeakerVolume);
    }

    [Fact]
    public void DeserializeSettings_WithMissingFields_ShouldUseDefaults()
    {
        var json = """{}""";
        var settings = JsonSerializer.Deserialize<AppSettings>(json, SafeOptions);
        Assert.NotNull(settings);
        Assert.Equal("http://localhost:5235", settings.ApiBaseUrl);
        Assert.Equal("http://localhost:5235", settings.ApiEndpoint);
        Assert.Null(settings.AuthToken);
        Assert.Null(settings.RefreshToken);
        Assert.Equal(0.01f, settings.VoiceDetectionThreshold);
    }
}
