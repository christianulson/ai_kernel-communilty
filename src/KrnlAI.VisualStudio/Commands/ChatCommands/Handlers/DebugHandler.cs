using System.Linq;
using System.Text;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class DebugHandler
{
    public static SlashCommand Create(IVsOperationTracker tracker) =>
        new("debug", "Show/clear internal debug trace of extension operations",
            async (args, ct) =>
            {
                await Task.Yield();
                var parts = args.Trim().Split([' '], StringSplitOptions.RemoveEmptyEntries);
                var subcommand = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";

                switch (subcommand)
                {
                    case "clear":
                        tracker.Clear();
                        return "Debug history cleared.";

                    case "help":
                    case "--help":
                    case "-h":
                        return GetUsage();

                    default:
                        var limit = 0;
                        if (!string.IsNullOrEmpty(subcommand) && int.TryParse(subcommand, out limit))
                        {
                            return FormatOperations(tracker.History, limit);
                        }
                        return FormatOperations(tracker.History, 0);
                }
            },
            () => true);

    private static string FormatOperations(IReadOnlyList<VsOperationCall> history, int limit)
    {
        if (history.Count == 0)
            return "No operations tracked yet. Use any extension feature to generate operations.";

        var sb = new StringBuilder();
        sb.AppendLine("### Debug Trace\n");

        var ops = limit > 0
            ? history.Skip(Math.Max(0, history.Count - limit)).ToList()
            : [.. history];

        foreach (var op in ops)
        {
            AppendOperation(sb, op, 0);
        }

        sb.AppendLine($"\n**Total:** {history.Count} operation(s) | Showing: {ops.Count}");
        return sb.ToString();
    }

    private static void AppendOperation(StringBuilder sb, VsOperationCall op, int indent)
    {
        var prefix = new string(' ', indent * 2);
        var icon = op.State switch
        {
            VsOperationState.Running => "⏳",
            VsOperationState.Completed => "✅",
            VsOperationState.Failed => "❌",
            VsOperationState.Cancelled => "🚫",
            _ => "❓"
        };

        var elapsed = op.ElapsedMs >= 1000
            ? $"{op.ElapsedMs / 1000.0:F1}s"
            : $"{op.ElapsedMs}ms";

        sb.AppendLine($"{prefix}{icon} **{op.Name}** — {op.State} ({elapsed})");

        if (op.Arguments is not null)
            sb.AppendLine($"{prefix}  Args: `{op.Arguments}`");

        if (op.Result is not null)
            sb.AppendLine($"{prefix}  Result: `{op.Result}`");

        if (op.Error is not null)
            sb.AppendLine($"{prefix}  Error: `{op.Error}`");

        if (op.Children is not null)
        {
            foreach (var child in op.Children)
            {
                AppendOperation(sb, child, indent + 1);
            }
        }
    }

    private static string GetUsage()
    {
        return """
### /debug — Usage

- `/debug` — Show all tracked operations
- `/debug <N>` — Show last N operations
- `/debug clear` — Clear all tracked operations
- `/debug help` — Show this help
""";
    }
}
