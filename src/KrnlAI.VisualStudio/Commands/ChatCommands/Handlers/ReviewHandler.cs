using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class ReviewHandler
{
    public static SlashCommand Create(IKernelClientService client, ISolutionContextService context) =>
        new("review", "Review the current file for issues",
            async (args, ct) =>
            {
                var selection = context.GetActiveSelection();
                var filePath = selection?.FilePath ?? context.GetActiveFilePath();
                if (string.IsNullOrWhiteSpace(filePath))
                    return "No file open. Open a file in the editor first.";

                var code = selection?.SurroundingContext ?? args;
                if (string.IsNullOrWhiteSpace(code))
                    return "Could not read file content.";

                var prompt = $"Review this code for bugs, performance issues, and code quality:\n\n```{selection?.Language ?? ""}\n{code}\n```";
                var result = await client.RunAgentAsync(prompt, ct: ct);
                return result?.Summary ?? result?.Status ?? "No review available.";
            });
}
