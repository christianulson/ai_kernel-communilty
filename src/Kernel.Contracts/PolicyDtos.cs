namespace Kernel.Contracts;

/// <summary>
/// DTO de entrada de política aprendida.
/// </summary>
/// <param name="Domain">Domínio da política</param>
/// <param name="ActionType">Tipo de ação</param>
/// <param name="Scenario">Código do cenário</param>
/// <param name="SuccessRate">Taxa de sucesso (0-1)</param>
/// <param name="AvgDeltaImprovement">Melhoria média medida</param>
/// <param name="Samples">Número de amostras</param>
public sealed record PolicyEntryDto(
    string Domain,
    string ActionType,
    string Scenario,
    double SuccessRate,
    double AvgDeltaImprovement,
    int Samples
);

/// <summary>
/// DTO de snapshot de políticas.
/// </summary>
/// <param name="CapturedAt">Timestamp da captura</param>
/// <param name="Policies">Lista de políticas</param>
public sealed record PolicySnapshotDto(
    DateTimeOffset CapturedAt,
    IReadOnlyList<PolicyEntryDto> Policies
);

public sealed record PolicyVersionDto(
    long Version,
    string Domain,
    string ActionType,
    string Scenario,
    double SuccessRate,
    double AvgDeltaImprovement,
    int Samples,
    DateTimeOffset CreatedAt
);

public sealed record PolicyRollbackRequest(long Version, string? Reason = null);

public sealed record PolicyRollbackAuditDto(
    long RollbackId,
    long TargetVersion,
    string PerformedBy,
    string? Reason,
    DateTimeOffset CreatedAt
);


public sealed record PolicyRollbackAuditDetailedDto(
    long RollbackId,
    string Domain,
    string ActionType,
    string Scenario,
    long TargetVersion,
    string PerformedBy,
    string? Reason,
    DateTimeOffset CreatedAt
);
