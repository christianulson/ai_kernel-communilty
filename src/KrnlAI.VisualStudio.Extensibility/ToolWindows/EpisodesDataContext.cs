using System.Runtime.Serialization;
using KrnlAI.VisualStudio.Extensibility.Core.Episodes;
using Microsoft.VisualStudio.Extensibility.UI;

namespace KrnlAI.VisualStudio.Extensibility.ToolWindows;

/// <summary>Data context for the episodes tool window (Remote UI).</summary>
[DataContract]
public sealed class EpisodesDataContext : NotifyPropertyChangedObject
{
    private readonly EpisodesModel _model;
    private string _input = string.Empty;

    /// <summary>Creates a new instance wrapping the given model.</summary>
    public EpisodesDataContext(EpisodesModel model)
    {
        _model = model;
        Items = new ObservableList<EpisodeEntry>(model.Episodes);
        AddCommand = new AsyncCommand(async (parameter, cancellationToken) =>
        {
            var text = Input;
            if (string.IsNullOrWhiteSpace(text))
                return;

            _model.AddEpisode(text);
            Input = string.Empty;
            Items.Add(new EpisodeEntry(text));
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
    public ObservableList<EpisodeEntry> Items { get; }

    /// <summary>Add command bound to the UI button.</summary>
    [DataMember]
    public AsyncCommand AddCommand { get; }
}