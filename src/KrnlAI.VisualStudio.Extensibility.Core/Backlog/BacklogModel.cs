using System.Runtime.Serialization;

namespace KrnlAI.VisualStudio.Extensibility.Core.Backlog;

/// <summary>A single backlog item.</summary>
[DataContract]
public sealed record BacklogItem([property: DataMember] string Title);

/// <summary>Pure backlog state (UI-agnostic).</summary>
public sealed class BacklogModel
{
    private readonly List<BacklogItem> _items = new();

    /// <summary>Backlog items.</summary>
    public IReadOnlyList<BacklogItem> Items => _items;

    /// <summary>Adds a new item and returns its index.</summary>
    public int AddItem(string title)
    {
        _items.Add(new BacklogItem(title));
        return _items.Count - 1;
    }
}