using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.Chat;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>
/// Data context for the Krnl-AI chat tool window (Remote UI).
/// </summary>
[DataContract]
public sealed class ChatDataContext : NotifyPropertyChangedObject
{
    private readonly ChatSession _session;
    private string _input = string.Empty;

    /// <summary>Creates a new instance wrapping the given session.</summary>
    public ChatDataContext(ChatSession session)
    {
        _session = session;
        Messages = new ObservableList<ChatMessage>(session.Messages);
        SendCommand = new AsyncCommand(async (parameter, cancellationToken) =>
        {
            var text = Input;
            if (string.IsNullOrWhiteSpace(text))
                return;

            _session.SendMessage(text);
            Input = string.Empty;
            Messages.Add(new ChatMessage("user", text));
        });
    }

    /// <summary>User input text.</summary>
    [DataMember]
    public string Input
    {
        get => _input;
        set => SetProperty(ref _input, value);
    }

    /// <summary>Visible chat messages.</summary>
    [DataMember]
    public ObservableList<ChatMessage> Messages { get; }

    /// <summary>Send command bound to the UI button.</summary>
    [DataMember]
    public AsyncCommand SendCommand { get; }
}