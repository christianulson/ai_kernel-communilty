using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class TestHandler
{
    public static SlashCommand Create(IKernelClientService client, ISolutionContextService context) =>
        new("test", "Generate unit tests for the current selection",
            async (args, ct) =>
            {
                var selection = context.GetActiveSelection();
                var code = selection?.SelectedText ?? args;
                if (string.IsNullOrWhiteSpace(code))
                    return "No code selected. Select code to test or provide code as argument.";

                var prompt = $"Generate xUnit tests for this code:\n\n```{selection?.Language ?? ""}\n{code}\n```";
                var result = await client.RunAgentAsync(prompt, ct: ct);
                return result?.Summary ?? result?.Status ?? "No tests generated.";
            });
}
