using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.Backlog;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Data context for the backlog tool window (Remote UI).</summary>
[DataContract]
public sealed class BacklogDataContext : NotifyPropertyChangedObject
{
    private readonly BacklogModel _model;
    private string _input = string.Empty;

    /// <summary>Creates a new instance wrapping the given model.</summary>
    public BacklogDataContext(BacklogModel model)
    {
        _model = model;
        Items = new ObservableList<BacklogItem>(model.Items);
        AddCommand = new AsyncCommand(async (parameter, cancellationToken) =>
        {
            var text = Input;
            if (string.IsNullOrWhiteSpace(text))
                return;

            _model.AddItem(text);
            Input = string.Empty;
            Items.Add(new BacklogItem(text));
        });
    }

    /// <summary>User input text.</summary>
    [DataMember]
    public string Input
    {
        get => _input;
        set => SetProperty(ref _input, value);
    }

    /// <summary>Visible items.</summary>
    [DataMember]
    public ObservableList<BacklogItem> Items { get; }

    /// <summary>Add command bound to the UI button.</summary>
    [DataMember]
    public AsyncCommand AddCommand { get; }
}