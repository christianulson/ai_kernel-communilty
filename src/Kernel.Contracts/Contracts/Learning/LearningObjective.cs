namespace LLMGateway.Api.Contracts.Learning;

public record LearningObjective(
    string ObjectiveId,
    string Question,
    string Domain,
    string GapType,
    double ExpectedValue,
    double AcquisitionCost,
    double Priority,
    string Status,
    DateTimeOffset CreatedAt
);

public record CuriositySignal(
    string SignalId,
    string Domain,
    string GapType,
    string Question,
    double ExpectedValue,
    double Urgency
);