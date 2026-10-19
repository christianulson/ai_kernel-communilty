using System.Collections.ObjectModel;

using System.Runtime.Serialization;

namespace KrnlAI.VisualStudio.Extensibility.Core.Chat;

/// <summary>Destination for chat messages (kernel API, log, etc.).</summary>
public interface IMessageSink
{
    /// <summary>Sends a chat message.</summary>
    Task SendAsync(string message, CancellationToken cancellationToken);
}

/// <summary>A single chat message.</summary>
/// <param name="Author">Message author ("user" or "assistant").</param>
/// <param name="Text">Message body.</param>
[DataContract]
public sealed record ChatMessage(
    [property: DataMember] string Author,
    [property: DataMember] string Text);

/// <summary>
/// Pure chat session state and send/receive logic (UI-agnostic).
/// </summary>
public sealed class ChatSession
{
    private readonly IMessageSink _sink;
    private readonly List<ChatMessage> _messages = new();

    /// <summary>Creates a new instance.</summary>
    public ChatSession(IMessageSink sink)
    {
        _sink = sink;
    }

    /// <summary>Messages in chronological order.</summary>
    public IReadOnlyList<ChatMessage> Messages => _messages;

    /// <summary>Sends a user message to the sink.</summary>
    public void SendMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _messages.Add(new ChatMessage("user", text));
        _ = _sink.SendAsync(text, CancellationToken.None);
    }

    /// <summary>Appends an assistant message.</summary>
    public void ReceiveMessage(string text)
    {
        _messages.Add(new ChatMessage("assistant", text));
    }

    /// <summary>Clears all messages.</summary>
    public void Clear()
    {
        _messages.Clear();
    }
}