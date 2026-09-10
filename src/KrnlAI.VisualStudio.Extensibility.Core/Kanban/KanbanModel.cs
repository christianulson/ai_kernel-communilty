using System.Runtime.Serialization;

namespace KrnlAI.VisualStudio.Extensibility.Core.Kanban;

/// <summary>A single kanban column.</summary>
[DataContract]
public sealed record KanbanColumn([property: DataMember] string Name);

/// <summary>Pure kanban board state (UI-agnostic).</summary>
public sealed class KanbanModel
{
    private readonly List<KanbanColumn> _columns = new();

    /// <summary>Board columns.</summary>
    public IReadOnlyList<KanbanColumn> Columns => _columns;

    /// <summary>Adds a new column.</summary>
    public void AddColumn(string name)
    {
        _columns.Add(new KanbanColumn(name));
    }
}