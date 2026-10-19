using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.Debug;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Data context for the debug tool window (Remote UI).</summary>
[DataContract]
public sealed class DebugDataContext : NotifyPropertyChangedObject
{
    private readonly DebugModel _model;
    private string _input = string.Empty;

    /// <summary>Creates a new instance wrapping the given model.</summary>
    public DebugDataContext(DebugModel model)
    {
        _model = model;
        Items = new ObservableList<DebugEntry>(model.Entries);
        AddCommand = new AsyncCommand(async (parameter, cancellationToken) =>
        {
            var text = Input;
            if (string.IsNullOrWhiteSpace(text))
                return;

            _model.AddEntry(text);
            Input = string.Empty;
            Items.Add(new DebugEntry(text));
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
    public ObservableList<DebugEntry> Items { get; }

    /// <summary>Add command bound to the UI button.</summary>
    [DataMember]
    public AsyncCommand AddCommand { get; }
}