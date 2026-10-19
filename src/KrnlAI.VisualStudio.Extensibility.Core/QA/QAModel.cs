using System.Runtime.Serialization;

namespace KrnlAI.VisualStudio.Extensibility.Core.QA;

/// <summary>A single QA case.</summary>
[DataContract]
public sealed record QaCase([property: DataMember] string Name);

/// <summary>Pure QA state (UI-agnostic).</summary>
public sealed class QAModel
{
    private readonly List<QaCase> _cases = new();

    /// <summary>QA cases.</summary>
    public IReadOnlyList<QaCase> Cases => _cases;

    /// <summary>Adds a new case and returns its index.</summary>
    public int AddCase(string name)
    {
        _cases.Add(new QaCase(name));
        return _cases.Count - 1;
    }
}