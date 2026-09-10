using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.Kanban;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Data context for the kanban tool window (Remote UI).</summary>
[DataContract]
public sealed class KanbanDataContext : NotifyPropertyChangedObject
{
    private readonly KanbanModel _model;
    private string _input = string.Empty;

    /// <summary>Creates a new instance wrapping the given model.</summary>
    public KanbanDataContext(KanbanModel model)
    {
        _model = model;
        Items = new ObservableList<KanbanColumn>(model.Columns);
        AddCommand = new AsyncCommand(async (parameter, cancellationToken) =>
        {
            var text = Input;
            if (string.IsNullOrWhiteSpace(text))
                return;

            _model.AddColumn(text);
            Input = string.Empty;
            Items.Add(new KanbanColumn(text));
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
    public ObservableList<KanbanColumn> Items { get; }

    /// <summary>Add command bound to the UI button.</summary>
    [DataMember]
    public AsyncCommand AddCommand { get; }
}