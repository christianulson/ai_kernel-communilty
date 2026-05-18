using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Tests.Models;

public sealed class MediaModelsTests
{
    [Fact]
    public void MediaDevice_ShouldSetProperties()
    {
        var device = new MediaDevice("mic-1", "Microphone Array", MediaDeviceType.AudioInput);

        Assert.Equal("mic-1", device.Id);
        Assert.Equal("Microphone Array", device.Name);
        Assert.Equal(MediaDeviceType.AudioInput, device.Type);
    }

    [Fact]
    public void MediaDeviceType_Enum_ShouldHaveExpectedValues()
    {
        Assert.Equal(0, (int)MediaDeviceType.AudioInput);
        Assert.Equal(1, (int)MediaDeviceType.AudioOutput);
        Assert.Equal(2, (int)MediaDeviceType.VideoInput);
    }

    [Fact]
    public void AppSettings_Default_ShouldHaveExpectedDefaults()
    {
        var settings = new AppSettings();

        Assert.Null(settings.SelectedMicrophoneId);
        Assert.False(settings.ContinuousListeningEnabled);
        Assert.Equal(0.01f, settings.VoiceDetectionThreshold);
        Assert.Equal(1500, settings.SilenceDurationMs);
        Assert.Equal("http://localhost:5000", settings.ApiBaseUrl);
        Assert.Equal(1.0f, settings.SpeakerVolume);
        Assert.False(settings.IsAuthenticated);
        Assert.Equal(1200, settings.WindowWidth);
        Assert.Equal(800, settings.WindowHeight);
        Assert.False(settings.WindowMaximized);
        Assert.Equal("dark", settings.Theme);
    }

    [Fact]
    public void AppSettings_WithValues_ShouldSetProperties()
    {
        var settings = new AppSettings
        {
            SelectedMicrophoneId = "mic-1",
            ContinuousListeningEnabled = true,
            VoiceDetectionThreshold = 0.05f,
            ApiBaseUrl = "https://api.example.com",
            IsAuthenticated = true,
            Theme = "light"
        };

        Assert.Equal("mic-1", settings.SelectedMicrophoneId);
        Assert.True(settings.ContinuousListeningEnabled);
        Assert.Equal(0.05f, settings.VoiceDetectionThreshold);
        Assert.Equal("https://api.example.com", settings.ApiBaseUrl);
        Assert.True(settings.IsAuthenticated);
        Assert.Equal("light", settings.Theme);
    }

    [Fact]
    public void AppSettings_WindowPosition_ShouldDefaultToNaN()
    {
        var settings = new AppSettings();

        Assert.True(double.IsNaN(settings.WindowLeft));
        Assert.True(double.IsNaN(settings.WindowTop));
    }

    [Fact]
    public void AppSettings_AuthToken_ShouldDefaultToNull()
    {
        var settings = new AppSettings();

        Assert.Null(settings.AuthToken);
        Assert.Null(settings.Username);
        Assert.Null(settings.TokenExpiresAt);
    }
}
