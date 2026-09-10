using System.Runtime.Serialization;

namespace KrnlAI.VisualStudio.Extensibility.Core.Policies;

/// <summary>A single policy entry.</summary>
[DataContract]
public sealed record PolicyEntry([property: DataMember] string Name);

/// <summary>Pure policies state (UI-agnostic).</summary>
public sealed class PoliciesModel
{
    private readonly List<PolicyEntry> _policies = new();

    /// <summary>Policy entries.</summary>
    public IReadOnlyList<PolicyEntry> Policies => _policies;

    /// <summary>Adds a new policy.</summary>
    public void AddPolicy(string name) { _policies.Add(new PolicyEntry(name)); }
}
