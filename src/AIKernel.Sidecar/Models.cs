namespace AIKernel.Sidecar;

public record AgentRunRequest(string? Prompt, string? Mode = "standalone");

public class AgentRunResponse
{
    public string? Narration { get; init; }
    public Dictionary<string, object>? Command { get; init; }
    public TransportStepDto[]? TransportSteps { get; init; }
    public string[]? ActiveStages { get; init; }
    public string? Error { get; init; }
}

public record TransportStepDto
{
    public string Label { get; init; } = "";
    public string Detail { get; init; } = "";
    public bool Ok { get; init; }
}
