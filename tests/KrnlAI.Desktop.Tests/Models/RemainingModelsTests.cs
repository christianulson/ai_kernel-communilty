namespace KrnlAI.Desktop.Tests.Models;

using AudioArgs = AudioCaptureEventArgs;
using VideoArgs = VideoCaptureEventArgs;
using ListenArgs = ListeningEventArgs;

public class AgentInfoTests
{
    [Fact]
    public void AgentInfo_ShouldCreate()
    {
        var agent = new AgentInfo("gpt4", "GPT-4", "OpenAI model");
        Assert.Equal("gpt4", agent.Id);
        Assert.Equal("GPT-4", agent.Name);
        Assert.Equal("OpenAI model", agent.Description);
    }
}

public class ConversationSessionTests
{
    [Fact]
    public void ConversationSession_ShouldCreate()
    {
        var now = DateTime.Now;
        var session = new ConversationSession("s1", "Chat 1", now);
        Assert.Equal("s1", session.Id);
        Assert.Equal("Chat 1", session.Title);
        Assert.Equal(now, session.CreatedAt);
    }
}

public class AudioCaptureEventArgsTests
{
    [Fact]
    public void AudioCaptureEventArgs_ShouldCreate()
    {
        var args = new AudioArgs([1, 2, 3], 16000, 1, 16, TimeSpan.FromSeconds(2));
        Assert.Equal(16000, args.SampleRate);
    }
}

public class VideoCaptureEventArgsTests
{
    [Fact]
    public void VideoCaptureEventArgs_ShouldCreate()
    {
        var args = new VideoArgs([255, 0, 128], 1920, 1080, TimeSpan.FromSeconds(1));
        Assert.Equal(1920, args.Width);
    }
}

public class ListeningEventArgsTests
{
    [Fact]
    public void ListeningEventArgs_ShouldCreate()
    {
        var args = new ListenArgs([10, 20, 30], TimeSpan.FromSeconds(3));
        Assert.Equal(TimeSpan.FromSeconds(3), args.Duration);
    }
}
