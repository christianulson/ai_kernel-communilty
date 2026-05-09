namespace LLMGateway.Api.Contracts.Learning;

public sealed record LearningStrategyConfig(
    string StrategyId,
    string Name,
    double BeliefExtractionThreshold,
    double CausalLinkThreshold,
    double ProcedureExtractionRate,
    double PolicySignalSensitivity,
    int MaxBeliefsPerEpisode,
    bool EnableCrossUserTransfer,
    DateTimeOffset CreatedAt);

public sealed record LearningStrategyMetrics(
    string StrategyId,
    int TotalEpisodesProcessed,
    double AvgBeliefConfidence,
    double AvgCausalConfidence,
    int ProceduresExtracted,
    int PolicySignalsGenerated,
    double LearningEfficiency,
    DateTimeOffset LastUpdated);

public sealed record StrategyAdjustment(
    string StrategyId,
    string AdjustedParameter,
    double OldValue,
    double NewValue,
    string Reason);
