namespace Kernel.Contracts;

/// <summary>
/// Formal output verdict from the MetaCritic evaluation.
/// </summary>
public enum CriticVerdict
{
    /// <summary>Plan is safe and beneficial — proceed with execution.</summary>
    Approve,

    /// <summary>Plan has minor issues that can be automatically repaired.</summary>
    Repair,

    /// <summary>Insufficient information — ask user for clarification.</summary>
    AskUser,

    /// <summary>Run more simulation to better assess risk before deciding.</summary>
    SimulateMore,

    /// <summary>Plan is too risky or misaligned — reject outright.</summary>
    Reject
}
