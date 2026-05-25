using KrnlAI.Desktop.Core.Models;
using KrnlAI.Embedded;

namespace KrnlAI.Desktop.App.Services;

public sealed class EmbeddedKanbanService
{
    private readonly EmbeddedKrnlAI _kernel;

    public EmbeddedKanbanService(EmbeddedKrnlAI kernel)
    {
        _kernel = kernel;
    }

    public async Task<KanbanDisplay> GetKanbanAsync(
        int daysBack = 10,
        string? domain = null,
        double? minPriority = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var goals = await _kernel.GetKanbanGoalsAsync(ct);

        if (!string.IsNullOrWhiteSpace(search))
            goals = goals.Where(g => g.Description.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        if (minPriority.HasValue)
            goals = goals.Where(g => g.Priority >= minPriority.Value).ToList();

        if (daysBack < 365)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-daysBack);
            goals = goals.Where(g => g.CreatedAt >= cutoff).ToList();
        }

        var ordered = new[] { "pending", "active", "paused", "completed", "cancelled" };
        var columns = ordered
            .Select(key => (key, label: GetColumnLabel(key)))
            .Select(t =>
            {
                var colGoals = goals.Where(g => g.Status == t.key).ToList();
                return new KanbanColumnDisplay(
                    t.key, t.label,
                    colGoals.Select(g => new KanbanCardDisplay(
                        g.Id, g.Description, g.Status, 0, g.Priority,
                        null, g.CreatedAt, g.Deadline, null, null
                    )).ToList(),
                    colGoals.Count);
            })
            .Where(c => c.Cards.Count > 0)
            .ToList();

        var otherStatuses = goals
            .Where(g => !ordered.Contains(g.Status))
            .GroupBy(g => g.Status)
            .Select(g => new KanbanColumnDisplay(
                g.Key, g.Key,
                g.Select(goal => new KanbanCardDisplay(
                    goal.Id, goal.Description, goal.Status, 0, goal.Priority,
                    null, goal.CreatedAt, goal.Deadline, null, null
                )).ToList(),
                g.Count()))
            .ToList();

        columns.AddRange(otherStatuses);

        return new KanbanDisplay(
            columns,
            new KanbanMetadataDisplay(
                goals.Count, columns.Count,
                new KanbanFilterDisplay(daysBack, domain, minPriority, null, search)));
    }

    public Task<bool> MoveCardAsync(string cardId, string newStatus, CancellationToken ct = default)
    {
        return _kernel.MoveKanbanCardAsync(cardId, newStatus, ct);
    }

    private static string GetColumnLabel(string status) => status switch
    {
        "pending" => "To Do",
        "active" or "in_progress" => "In Progress",
        "completed" or "done" => "Done",
        "paused" => "Paused",
        "cancelled" => "Cancelled",
        _ => status
    };
}
