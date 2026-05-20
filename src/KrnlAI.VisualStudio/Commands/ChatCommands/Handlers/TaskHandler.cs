using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class TaskHandler
{
    public static SlashCommand Create(IKernelClientService client, ISolutionContextService context, IAgenticLoopService agenticLoop) =>
        new("task", "Execute a multi-step autonomous task",
            async (args, ct) =>
            {
                var goal = !string.IsNullOrWhiteSpace(args) ? args
                    : context.GetActiveSelection()?.SelectedText;

                if (string.IsNullOrWhiteSpace(goal))
                    return "Please provide a task description. Usage: /task <description>";

                var result = await agenticLoop.ExecuteAsync(goal!, ct);
                return result.Status switch
                {
                    "Completed" => $"✅ Task completed successfully.\n\n{result.Summary}",
                    "Failed" => $"❌ Task failed: {result.Error}",
                    "Cancelled" => "⏹ Task cancelled.",
                    _ => $"Task finished with status: {result.Status}",
                };
            });
}
