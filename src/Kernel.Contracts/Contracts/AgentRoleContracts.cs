namespace Kernel.Contracts;

public sealed record SpecializedAgentRole(
    string RoleId,
    string RoleName,
    string Description,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> AllowedTools,
    string DefaultMemoryScope,
    double BaseConfidence,
    string MaxRiskLevel,
    int MaxConcurrentTasks
);

/// <summary>
/// Resultado da validação do contrato de um papel especializado de agente.
/// </summary>
public sealed record AgentRoleContractValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);

/// <summary>
/// Valida invariantes mínimas para papéis especializados de agentes.
/// </summary>
public static class AgentRoleContractValidator
{
    /// <summary>
    /// Valida se o papel possui identidade, capacidades e allowlist de ferramentas.
    /// </summary>
    public static AgentRoleContractValidationResult Validate(SpecializedAgentRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(role.RoleId))
            errors.Add("role_id_required");

        if (string.IsNullOrWhiteSpace(role.RoleName))
            errors.Add("role_name_required");

        if (role.Capabilities is null || role.Capabilities.Count == 0 || role.Capabilities.All(string.IsNullOrWhiteSpace))
            errors.Add("capabilities_required");

        if (role.AllowedTools is null || role.AllowedTools.Count == 0 || role.AllowedTools.All(string.IsNullOrWhiteSpace))
            errors.Add("allowed_tools_required");

        if (role.MaxConcurrentTasks < 1)
            errors.Add("max_concurrent_tasks_must_be_positive");

        return new AgentRoleContractValidationResult(errors.Count == 0, errors);
    }
}

public sealed record AgentRoleAssignment(
    string AssignmentId,
    string RoleId,
    string Goal,
    string Status,
    DateTimeOffset AssignedAt,
    DateTimeOffset? CompletedAt
);

public sealed record RoleBasedTask(
    string TaskId,
    string Goal,
    string RequiredCapability,
    string? AssignedRoleId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);

public sealed record OrchestrationPlan(
    IReadOnlyList<RoleBasedTask> Tasks,
    string Strategy
);
