using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Tests.Models;

public class ChatMessageTests
{
    [Fact]
    public void ChatMessage_ShouldCreateWithCorrectProperties()
    {
        var timestamp = DateTime.Now;
        var message = new ChatMessage(
            "test-id",
            "Hello World",
            MessageRole.User,
            timestamp,
            MessageStatus.Pending
        );

        Assert.Equal("test-id", message.Id);
        Assert.Equal("Hello World", message.Content);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal(timestamp, message.Timestamp);
        Assert.Equal(MessageStatus.Pending, message.Status);
    }

    [Fact]
    public void ChatMessage_ShouldDefaultToPendingStatus()
    {
        var message = new ChatMessage(
            "id",
            "content",
            MessageRole.User,
            DateTime.Now
        );

        Assert.Equal(MessageStatus.Pending, message.Status);
    }

    [Theory]
    [InlineData(MessageRole.User)]
    [InlineData(MessageRole.Assistant)]
    [InlineData(MessageRole.System)]
    public void ChatMessage_ShouldSupportAllRoles(MessageRole role)
    {
        var message = new ChatMessage("id", "content", role, DateTime.Now);
        Assert.Equal(role, message.Role);
    }

    [Theory]
    [InlineData(MessageStatus.Pending)]
    [InlineData(MessageStatus.Processing)]
    [InlineData(MessageStatus.Completed)]
    [InlineData(MessageStatus.Error)]
    public void ChatMessage_ShouldSupportAllStatuses(MessageStatus status)
    {
        var message = new ChatMessage("id", "content", MessageRole.User, DateTime.Now, status);
        Assert.Equal(status, message.Status);
    }
}

public class MediaDeviceTests
{
    [Fact]
    public void MediaDevice_ShouldCreateWithCorrectProperties()
    {
        var device = new MediaDevice("device-1", "USB Microphone", MediaDeviceType.AudioInput);

        Assert.Equal("device-1", device.Id);
        Assert.Equal("USB Microphone", device.Name);
        Assert.Equal(MediaDeviceType.AudioInput, device.Type);
    }

    [Theory]
    [InlineData(MediaDeviceType.AudioInput)]
    [InlineData(MediaDeviceType.AudioOutput)]
    [InlineData(MediaDeviceType.VideoInput)]
    public void MediaDevice_ShouldSupportAllDeviceTypes(MediaDeviceType type)
    {
        var device = new MediaDevice("id", "Device", type);
        Assert.Equal(type, device.Type);
    }
}

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_ShouldHaveDefaultValues()
    {
        var settings = new AppSettings();

        Assert.Null(settings.SelectedMicrophoneId);
        Assert.Null(settings.SelectedCameraId);
        Assert.Null(settings.SelectedSpeakerId);
        Assert.Equal("http://localhost:5235", settings.ApiBaseUrl);
        Assert.Equal("http://localhost:5235", settings.ApiEndpoint);
        Assert.Equal(0.01f, settings.VoiceDetectionThreshold);
        Assert.Equal(1500, settings.SilenceDurationMs);
        Assert.Equal(1.0f, settings.SpeakerVolume);
    }

    [Fact]
    public void AppSettings_ShouldAllowCustomValues()
    {
        var settings = new AppSettings
        {
            SelectedMicrophoneId = "mic-1",
            SelectedCameraId = "cam-0",
            SelectedSpeakerId = "speaker-2",
            ApiBaseUrl = "http://192.168.1.100:5000",
            VoiceDetectionThreshold = 0.05f,
            SilenceDurationMs = 2000,
            SpeakerVolume = 0.8f
        };

        Assert.Equal("mic-1", settings.SelectedMicrophoneId);
        Assert.Equal("cam-0", settings.SelectedCameraId);
        Assert.Equal("speaker-2", settings.SelectedSpeakerId);
        Assert.Equal("http://192.168.1.100:5000", settings.ApiBaseUrl);
        Assert.Equal(0.05f, settings.VoiceDetectionThreshold);
        Assert.Equal(2000, settings.SilenceDurationMs);
        Assert.Equal(0.8f, settings.SpeakerVolume);
    }
}

public class AgentRunRequestTests
{
    [Fact]
    public void AgentRunRequest_ShouldCreateWithDefaults()
    {
        var request = new AgentRunRequest("Hello");

        Assert.Equal("Hello", request.Prompt);
        Assert.Equal("gateway", request.Mode);
        Assert.Null(request.AgentId);
        Assert.Null(request.Metadata);
    }

    [Fact]
    public void AgentRunRequest_ShouldAllowCustomValues()
    {
        var metadata = new Dictionary<string, string> { { "key", "value" } };
        var request = new AgentRunRequest("Test", "kernel", "agent-1", metadata);

        Assert.Equal("Test", request.Prompt);
        Assert.Equal("kernel", request.Mode);
        Assert.Equal("agent-1", request.AgentId);
        Assert.NotNull(request.Metadata);
        Assert.Equal("value", request.Metadata["key"]);
    }
}
