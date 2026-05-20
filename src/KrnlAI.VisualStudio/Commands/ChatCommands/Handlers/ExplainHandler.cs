using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class ExplainHandler
{
    public static SlashCommand Create(IKernelClientService client, ISolutionContextService context) =>
        new("explain", "Explain the current code selection",
            async (args, ct) =>
            {
                var selection = context.GetActiveSelection();
                var code = selection?.SelectedText ?? args;
                if (string.IsNullOrWhiteSpace(code))
                    return "No code selected. Select code in the editor or provide code as argument.";

                var prompt = $"Explain this code:\n\n```{selection?.Language ?? ""}\n{code}\n```";
                var result = await client.RunAgentAsync(prompt, ct: ct);
                return result?.Summary ?? result?.Status ?? "No explanation available.";
            });
}
