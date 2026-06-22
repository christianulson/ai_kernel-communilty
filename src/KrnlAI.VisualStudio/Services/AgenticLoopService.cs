using KrnlAI.Sdk.Models;

namespace KrnlAI.VisualStudio.Services;

public sealed class AgenticLoopService(
    IKernelClientService client,
    IVsOperationTracker? debugTracker = null) : IAgenticLoopService
{
    private readonly IVsOperationTracker _debugTracker = debugTracker ?? new VsOperationTracker();
    private CancellationTokenSource? _currentCts;

    public async Task<AgenticLoopResult> ExecuteAsync(string goal, CancellationToken ct)
    {
        using var op = _debugTracker.Start("agentic_loop.run", goal);

        _currentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            var request = new AgentRunRequest(
                Goal: goal,
                MaxSteps: 25,
                ApproveHighRisk: false,
                ApproveMetaCriticStops: false
            );

            var response = await client.RunAgentAsync(goal, request, _currentCts.Token);

            if (_currentCts.Token.IsCancellationRequested)
            {
                op.SetResult("Cancelled");
                return new AgenticLoopResult("Cancelled", null, null, null);
            }

            var steps = response.Steps?
                .Select((s, i) => new AgentStep(i + 1, s.Tool, s.Success ? "OK" : s.Error, s.Success))
                .ToList() as IReadOnlyList<AgentStep>;

            var status = response.Status ?? "Completed";
            op.SetResult(status);
            return new AgenticLoopResult(status, response.Summary, null, steps);
        }
        catch (OperationCanceledException)
        {
            op.SetResult("Cancelled");
            return new AgenticLoopResult("Cancelled", null, null, null);
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            return new AgenticLoopResult("Failed", null, ex.Message, null);
        }
    }

    public Task CancelAsync()
    {
        _currentCts?.Cancel();
        return Task.CompletedTask;
    }
}
