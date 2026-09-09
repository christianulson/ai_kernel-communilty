using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Chat;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Chat;

public sealed class ChatSessionTests
{
    private sealed class FakeSink : IMessageSink
    {
        public List<string> Received { get; } = new();

        public Task SendAsync(string message, CancellationToken cancellationToken)
        {
            Received.Add(message);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void SendMessage_ValidInput_ShouldSendToSinkAndAppendUserMessage()
    {
        var sink = new FakeSink();
        var session = new ChatSession(sink);

        session.SendMessage("hello");

        sink.Received.Should().ContainSingle().Which.Should().Be("hello");
        session.Messages.Should().ContainSingle(m => m.Author == "user" && m.Text == "hello");
    }

    [Fact]
    public void SendMessage_EmptyInput_ShouldNotSendOrAppend()
    {
        var sink = new FakeSink();
        var session = new ChatSession(sink);

        session.SendMessage("   ");

        sink.Received.Should().BeEmpty();
        session.Messages.Should().BeEmpty();
    }

    [Fact]
    public void ReceiveMessage_ShouldAppendAssistantMessage()
    {
        var session = new ChatSession(new FakeSink());

        session.ReceiveMessage("analysis result");

        session.Messages.Should().ContainSingle(m => m.Author == "assistant" && m.Text == "analysis result");
    }

    [Fact]
    public void Clear_ShouldRemoveAllMessages()
    {
        var session = new ChatSession(new FakeSink());
        session.SendMessage("a");
        session.ReceiveMessage("b");

        session.Clear();

        session.Messages.Should().BeEmpty();
    }
}