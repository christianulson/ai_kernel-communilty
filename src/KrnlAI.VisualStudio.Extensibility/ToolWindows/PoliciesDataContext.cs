using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.Policies;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Data context for the policies tool window (Remote UI).</summary>
[DataContract]
public sealed class PoliciesDataContext : NotifyPropertyChangedObject
{
    private readonly PoliciesModel _model;
    private string _input = string.Empty;

    /// <summary>Creates a new instance wrapping the given model.</summary>
    public PoliciesDataContext(PoliciesModel model)
    {
        _model = model;
        Items = new ObservableList<PolicyEntry>(model.Policies);
        AddCommand = new AsyncCommand(async (parameter, cancellationToken) =>
        {
            var text = Input;
            if (string.IsNullOrWhiteSpace(text))
                return;

            _model.AddPolicy(text);
            Input = string.Empty;
            Items.Add(new PolicyEntry(text));
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
    public ObservableList<PolicyEntry> Items { get; }

    /// <summary>Add command bound to the UI button.</summary>
    [DataMember]
    public AsyncCommand AddCommand { get; }
}