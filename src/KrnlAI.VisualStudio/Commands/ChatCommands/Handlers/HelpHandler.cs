using System.Text;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class HelpHandler
{
    public static SlashCommand Create(SlashCommandRouter router) =>
        new("help", "Show available commands",
            async (args, ct) =>
            {
                await Task.Yield();
                var sb = new StringBuilder();
                sb.AppendLine("### Available Commands\n");
                foreach (var cmd in router.GetVisibleCommands())
                {
                    sb.AppendLine($"- **/{cmd.Name}** — {cmd.Description}");
                }
                sb.AppendLine("\nType a command followed by arguments. For example:\n");
                sb.AppendLine("```");
                sb.AppendLine("/explain");
                sb.AppendLine("/fix");
                sb.AppendLine("/test CalculateTotal");
                sb.AppendLine("```");
                return sb.ToString();
            });
}
