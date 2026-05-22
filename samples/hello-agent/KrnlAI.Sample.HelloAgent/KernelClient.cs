using System.Net.Http.Json;
using KrnlAI.Sdk;

namespace KrnlAI.Sample.HelloAgent;

public sealed class KernelClient(HttpClient http)
{
    public async Task<AgentRunResult> RunAgentAsync(string goal, CancellationToken ct = default)
    {
        var payload = new { goal, userId = "sample-user" };
        var response = await http.PostAsJsonAsync("/agent/run", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AgentRunResult>(ct);
        return result ?? throw new InvalidOperationException("Empty response from kernel");
    }
}

public sealed record AgentRunResult(
    string Status,
    string Summary,
    AgentStep[]? Steps
);

public sealed record AgentStep(
    string Tool,
    bool Success,
    string? Error
);
