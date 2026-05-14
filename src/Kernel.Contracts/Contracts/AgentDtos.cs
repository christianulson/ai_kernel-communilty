using System.ComponentModel.DataAnnotations;

namespace Kernel.Contracts;

/// <summary>
/// Política de orçamento / governança para uma execução do agente.
/// </summary>
/// <param name="MaxToolCalls">Limite de chamadas de tools (steps) por execução.</param>
/// <param name="MaxTokens">Limite aproximado de tokens (heurístico).</param>
/// <param name="MaxLatencyMs">Limite de latência total em ms (heurístico).</param>
/// <param name="MaxEstimatedCost">Limite de custo estimado (heurístico, moeda abstrata).</param>
public sealed record BudgetPolicy(
    [Range(1, 20)] int MaxToolCalls = 12,
    [Range(256, 100_000)] int MaxTokens = 4000,
    [Range(1_000, 300_000)] int MaxLatencyMs = 30_000,
    [Range(typeof(decimal), "0", "1000")] decimal MaxEstimatedCost = 0m
);

/// <summary>
/// Snapshot de perfil do usuário para coerência e governança.
/// </summary>
public sealed record AgentProfileSnapshot(
    string UserId,
    string Language,
    string Tone,
    string RiskTolerance,
    IReadOnlyList<string> LongTermGoals,
    IReadOnlyList<string> PersonalRules
);

/// <summary>
/// Resultado do SelfCheck (metacognição antes de agir).
/// </summary>
public sealed record SelfCheckResult(
    double RiskScore,
    double UtilityScore,
    double ConsistencyScore,
    double Uncertainty,
    IReadOnlyList<string> MissingInfo,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> StopReasons,
    CriticVerdict Verdict = CriticVerdict.Approve
);

/// <summary>
/// Resultado do PostMortem (metacognição depois de agir).
/// </summary>
public sealed record PostMortemResult(
    IReadOnlyList<string> WhatWorked,
    IReadOnlyList<string> WhatFailed,
    IReadOnlyList<string> NextTimeDo
);

/// <summary>
/// Métricas de execução para auditoria e aprendizado.
/// </summary>
public sealed record ExecutionMetrics(
    int StepsPlanned,
    int StepsExecuted,
    int ToolCalls,
    int EstimatedTokens,
    int LatencyMs,
    decimal EstimatedCost,
    bool Success
);

/// <summary>
/// Versões estáveis dos prompts usados pelo fluxo /agent/run.
/// </summary>
public static class AgentRunPromptVersions
{
    public const string Planner = "agent-run-planner.v1";
}

/// <summary>
/// Metadados auditáveis do prompt/modelo usados para gerar e executar um episódio.
/// </summary>
public sealed record AgentRunPromptMetadata(
    string PromptCapability,
    string PromptVersion,
    string? Provider,
    string? Model);

/// <summary>
/// Request para executar um objetivo do agente.
/// </summary>
/// <param name="Goal">Objetivo em linguagem natural</param>
/// <param name="UserId">ID do usuário (para memória episódica e auditoria)</param>
/// <param name="MaxSteps">Número máximo de steps no plano (padrão: 6)</param>
/// <param name="Provider">
/// Provedor LLM: "rule" | "ollama" | "openai". Se null, usa DefaultProvider.
/// </param>
/// <param name="Model">
/// Modelo do provedor (ex.: "llama3", "gpt-5"). Se null, usa o configurado.
/// </param>
/// <param name="ApproveHighRisk">Se true, permite executar planos com steps de risco "high" (sujeito a allowlist).</param>
/// <param name="ApproveMetaCriticStops">Se true, permite continuar execução mesmo com StopReasons da meta-crítica.</param>
/// <param name="MetaCriticOverrideReason">Justificativa obrigatória (auditável) quando houver override da meta-crítica.</param>
/// <param name="Budget">Orçamento (quota) da execução.</param>
public sealed record AgentRunRequest(
    [MaxLength(600)] string Goal,
    [Required, MinLength(2), MaxLength(120)] string UserId = "system",
    [MaxLength(80)] string? GoalId = null,
    [Range(1, 12)] int MaxSteps = 6,
    [RegularExpression("^(rule|ollama|openai)$", ErrorMessage = "provider must be one of: rule, ollama, openai")]
    string? Provider = null,
    [MaxLength(120)] string? Model = null,
    bool ApproveHighRisk = false,
    bool ApproveMetaCriticStops = false,
    [MaxLength(240)] string? MetaCriticOverrideReason = null,
    BudgetPolicy? Budget = null,
    [Range(1, 5)] int MaxAutonomousLoops = 1
);

/// <summary>
/// Resposta da execução de um objetivo do agente.
/// </summary>
public sealed record AgentRunResponse(
    string Goal,
    string Status,
    string Summary,
    IReadOnlyList<PlanStepResult> Steps,
    string? EpisodeId,
    string? Provider = null,
    string? Model = null,
    SelfCheckResult? SelfCheck = null,
    PostMortemResult? PostMortem = null,
    ExecutionMetrics? Metrics = null,
    bool MetaCriticOverrideApplied = false,
    HierarchicalPlan? PlannerHierarchy = null,
    IReadOnlyList<string>? AgendaSuggestions = null,
    PlanningGovernanceSnapshot? PlanningGovernance = null,
    string? GoalId = null
);

/// <summary>
/// Comando parseado do agente (pode ser single, plan ou needMoreInfo).
/// </summary>
public sealed record AgentCommand(
    string Mode,
    double Confidence,
    string? NeedMoreInfo,
    AgentPlan? Plan,
    object? KernelCommand,
    string? Risk,
    string? RollbackPlan,
    string? CriticNotes
);

/// <summary>
/// Stable planner structured-output mode names used by prompts, parsers and validators.
/// </summary>
public static class PlannerStructuredOutputModes
{
    public const string Plan = "plan";
    public const string Single = "single";
    public const string NeedMoreInfo = "needMoreInfo";
}

/// <summary>
/// Metadata describing the structured-output contract expected from the planner.
/// </summary>
public sealed record PlannerStructuredOutputContract(
    string SchemaVersion,
    IReadOnlyList<string> AllowedModes,
    IReadOnlyList<string> RequiredTopLevelFields);

/// <summary>
/// Plano multi-step do agente.
/// </summary>
public sealed record AgentPlan(
    string Goal,
    IReadOnlyList<AgentPlanStep> Steps,
    DateTimeOffset? ExpectedCompletion = null,
    TimeSpan? TotalEstimatedDuration = null);

/// <summary>
/// Estrutura hierárquica do plano para governança/autonomia controlada.
/// </summary>
public sealed record HierarchicalPlan(string Goal, IReadOnlyList<HierarchicalPlanStage> Stages)
{
    public IReadOnlyList<HierarchicalPlanSubgoal> Subgoals { get; init; } = [];
}

/// <summary>
/// Subgoal derivado do plano hierárquico com dependências explícitas.
/// </summary>
/// <param name="Id">Identificador estável do subgoal.</param>
/// <param name="Goal">Objetivo do subgoal.</param>
/// <param name="DependsOn">IDs de subgoals predecessores.</param>
/// <param name="StepCount">Quantidade de steps cobertos.</param>
/// <param name="MaxRisk">Maior risco observado no subgoal.</param>
public sealed record HierarchicalPlanSubgoal(
    string Id,
    string Goal,
    IReadOnlyList<string> DependsOn,
    int StepCount,
    string MaxRisk);

public sealed record HierarchicalPlanStage(string Id, string Goal, int StepCount, string MaxRisk);

public sealed record PlanningGovernanceSnapshot(int HierarchicalStageSize, int AgendaSuggestionLimit);


/// <summary>
/// Step de um plano do agente.
/// </summary>
public sealed record AgentPlanStep(
    string Tool,
    object Input,
    string ExpectedOutcome,
    string Risk,
    string? RollbackHint,
    string? IdempotencyKey = null,
    TimeSpan? EstimatedDuration = null
);

/// <summary>
/// Resultado da execução de um step do plano.
/// </summary>
public sealed record PlanStepResult(
    int Index,
    string Tool,
    string Risk,
    string? RollbackHint,
    object RawResult,
    bool Success = true,
    string? Error = null,
    string? IdempotencyKey = null
);

public sealed record AgentFeedbackRequest(
    string EpisodeId,
    int Rating,
    string? Comment,
    string? Correction,
    string? PreferredPlan
);

public sealed record AgentFeedbackResponse(
    string FeedbackId,
    string EpisodeId,
    int Rating,
    bool Recorded,
    DateTimeOffset RecordedAt
);
