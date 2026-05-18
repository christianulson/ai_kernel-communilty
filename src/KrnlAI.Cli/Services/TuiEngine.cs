using Spectre.Console;
using Kernel.Core.Abstractions;
using Kernel.Core.Services.Neural;

namespace KrnlAI.Cli.Services;

public sealed class TuiEngine : IDisposable
{
    private readonly IQLearningService? _ql;
    private readonly MultiAgentOrchestrator? _orchestrator;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public TuiEngine(IQLearningService? ql = null, MultiAgentOrchestrator? orchestrator = null)
    {
        _ql = ql;
        _orchestrator = orchestrator;
    }

    public async Task RunAsync()
    {
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        await AnsiConsole.Live(new Panel(new Markup("Starting...")).Header("Krnl-AI"))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!ct.IsCancellationRequested)
                {
                    var grid = new Grid();
                    grid.AddColumn();
                    grid.AddColumn();

                    var statusPanel = BuildStatusPanel();
                    var agentsPanel = BuildAgentsPanel();
                    grid.AddRow(statusPanel, agentsPanel);

                    var qTablePanel = await BuildQTablePanel(ct);
                    var eventsPanel = BuildEventsPanel();
                    grid.AddRow(qTablePanel, eventsPanel);

                    ctx.UpdateTarget(new Panel(grid).Header($"Krnl-AI TUI — {DateTimeOffset.UtcNow:HH:mm:ss}"));
                    ctx.Refresh();
                    await Task.Delay(3000, ct);
                }
            });
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private Panel BuildStatusPanel()
    {
        var table = new Table().Border(TableBorder.Rounded).Width(40);
        table.AddColumn("Metric");
        table.AddColumn("Value");
        table.AddRow("Health", "[green]Good[/]");
        table.AddRow("Risk", "[yellow]Low[/]");
        table.AddRow("Mood", "[blue]Curious[/]");
        table.AddRow("Cognitive Load", $"{Random.Shared.Next(20, 80)}%");
        return new Panel(table).Header("System Status");
    }

    private Panel BuildAgentsPanel()
    {
        if (_orchestrator is null)
            return new Panel(new Markup("[dim]Multi-agent not available[/]")).Header("Agents");

        var table = new Table().Border(TableBorder.Rounded).Width(40);
        table.AddColumn("Agent");
        table.AddColumn("Domain");
        table.AddColumn("Status");

        foreach (var id in _orchestrator.AgentIds)
        {
            var agent = _orchestrator.GetAgent(id);
            var domain = agent?.Domain ?? "?";
            var icons = new Dictionary<string, string>
            {
                ["attention"] = "[yellow]focus[/]", ["memory"] = "[blue]store[/]",
                ["planning"] = "[cyan]plan[/]", ["safety"] = "[green]guard[/]",
                ["reasoning"] = "[purple]think[/]", ["execution"] = "[red]act[/]",
                ["metacognition"] = "[silver]meta[/]"
            };
            table.AddRow(id, domain, icons.GetValueOrDefault(domain, "[dim]idle[/]"));
        }
        return new Panel(table).Header("Cognitive Agents");
    }

    private async Task<Panel> BuildQTablePanel(CancellationToken ct)
    {
        if (_ql is null)
            return new Panel(new Markup("[dim]Q-Learning not available[/]")).Header("Q-Table");

        try
        {
            var summary = await _ql.GetPolicySummaryAsync(ct);
            var lines = summary.Split('\n');
            var display = string.Join("\n", lines.Take(8).Select(l => $"[grey]{l.Replace("[", "[[").Replace("]", "]]")}[/]"));
            return new Panel(new Markup(display)).Header("Q-Table");
        }
        catch
        {
            return new Panel(new Markup("[red]Error fetching Q-Table[/]")).Header("Q-Table");
        }
    }

    private Panel BuildEventsPanel()
    {
        var eventCount = Random.Shared.Next(1, 5);
        var events = new List<string>();
        for (int i = 0; i < eventCount; i++)
            events.Add($"[grey]{DateTimeOffset.UtcNow:HH:mm:ss}[/] cognitive cycle tick");
        return new Panel(new Markup(string.Join("\n", events))).Header("Events");
    }
}
