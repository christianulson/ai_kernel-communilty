using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.QA;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Data context for the qa tool window (Remote UI).</summary>
[DataContract]
public sealed class QADataContext : NotifyPropertyChangedObject
{
    private readonly QAModel _model;
    private string _input = string.Empty;

    /// <summary>Creates a new instance wrapping the given model.</summary>
    public QADataContext(QAModel model)
    {
        _model = model;
        Items = new ObservableList<QaCase>(model.Cases);
        AddCommand = new AsyncCommand(async (parameter, cancellationToken) =>
        {
            var text = Input;
            if (string.IsNullOrWhiteSpace(text))
                return;

            _model.AddCase(text);
            Input = string.Empty;
            Items.Add(new QaCase(text));
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
    public ObservableList<QaCase> Items { get; }

    /// <summary>Add command bound to the UI button.</summary>
    [DataMember]
    public AsyncCommand AddCommand { get; }
}