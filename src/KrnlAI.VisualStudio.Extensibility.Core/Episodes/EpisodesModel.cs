using System.Runtime.Serialization;

namespace KrnlAI.VisualStudio.Extensibility.Core.Episodes;

/// <summary>A single episode.</summary>
[DataContract]
public sealed record EpisodeEntry([property: DataMember] string Name);

/// <summary>Pure episodes state (UI-agnostic).</summary>
public sealed class EpisodesModel
{
    private readonly List<EpisodeEntry> _episodes = new();

    /// <summary>Episode entries.</summary>
    public IReadOnlyList<EpisodeEntry> Episodes => _episodes;

    /// <summary>Adds a new episode.</summary>
    public void AddEpisode(string name) { _episodes.Add(new EpisodeEntry(name)); }
}
