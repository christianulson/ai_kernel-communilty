using KrnlAI.Contracts;
using Spectre.Console;

namespace KrnlAI.Cli.Services;


/// <summary>Renders a Kanban board to the terminal using Spectre.Console.</summary>
public sealed class KanbanRenderer(IAnsiConsole console)
{
    public IAnsiConsole Console => console;

    public void OutputError(string message) => console.MarkupLine(message);

    public void Render(KanbanResponse data)
    {
        if (data.Columns.Count == 0)
        {
            console.MarkupLine("[yellow]No goals found for the current filters[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded);

        foreach (var column in data.Columns)
            table.AddColumn(new TableColumn($"{GetIcon(column.Column)} {column.Label} ({column.TotalCount})"));

        var maxRows = data.Columns.Max(c => c.Cards.Count);
        for (var i = 0; i < maxRows; i++)
        {
            var cells = data.Columns.Select(col =>
                i < col.Cards.Count ? RenderCard(col.Cards[i]) : "").ToArray();
            table.AddRow(cells);
        }

        console.Write(table);

        var meta = data.Metadata;
        var footer = $"[dim]Total: {meta.TotalGoals} goals across {meta.TotalColumns} columns[/]";
        if (meta.Filters.DaysBack != 10)
            footer += $" | [dim]Days back: {meta.Filters.DaysBack}[/]";
        if (meta.Filters.Domain is not null)
            footer += $" | [dim]Domain: {meta.Filters.Domain}[/]";
        if (meta.Filters.MinPriority.HasValue)
            footer += $" | [dim]Min priority: {meta.Filters.MinPriority}[/]";
        if (meta.Filters.Search is not null)
            footer += $" | [dim]Search: {meta.Filters.Search}[/]";
        console.MarkupLine($"\n{footer}");
    }

    private static string RenderCard(KanbanCard card)
    {
        var color = card.Status switch
        {
            "active" when card.Progress is 0 or -0 => "grey",
            "active" => "yellow",
            "blocked" => "red",
            "completed" => "green",
            "failed" or "abandoned" => "red dim",
            _ => "grey"
        };

        var lines = new List<string>
        {
            $"[{color}][bold]P{card.Priority}[/] {card.Progress:P0}[/]",
            $"[{color}]{(card.Description.Length > 30 ? card.Description[..30] + "..." : card.Description).EscapeMarkup()}[/]"
        };

        if (card.Deadline.HasValue)
            lines.Add($"[{color}]📅 {card.Deadline:yyyy-MM-dd}[/]");

        if (card.SubGoals is { Count: > 0 })
            lines.Add($"[dim]{GetIcon("sub")} {card.SubGoals.Count} sub-goals[/]");

        return string.Join("\n", lines);
    }

    private static string GetIcon(string column) => column switch
    {
        "backlog" => "📋",
        "in_progress" => "🔧",
        "blocked" => "⛔",
        "done" => "✅",
        "failed" => "❌",
        "sub" => "⊢",
        _ => ""
    };
}
