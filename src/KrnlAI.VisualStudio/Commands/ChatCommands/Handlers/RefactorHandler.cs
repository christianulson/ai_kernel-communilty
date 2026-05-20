using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class RefactorHandler
{
    public static SlashCommand Create(IKernelClientService client, ISolutionContextService context, IApplyEditService applyEdit) =>
        new("refactor", "Refactor the current code selection",
            async (args, ct) =>
            {
                var selection = context.GetActiveSelection();
                var code = selection?.SelectedText ?? args;
                if (string.IsNullOrWhiteSpace(code))
                    return "No code selected. Select code to refactor or provide code as argument.";

                var prompt = $"Refactor this code to improve quality while preserving behavior:\n\n```{selection?.Language ?? ""}\n{code}\n```";
                var result = await client.RunAgentAsync(prompt, ct: ct);
                var refactored = result?.Summary ?? result?.Status;
                if (string.IsNullOrWhiteSpace(refactored))
                    return "No refactoring suggestions available.";

                var diff = $"- Original:\n{code}\n+ Refactored:\n{refactored}";
                var applied = await applyEdit.PreviewAndApplyAsync(diff, ct);
                return applied
                    ? "Refactoring applied. Review the changes."
                    : $"Refactoring cancelled.\n\n--- Suggested refactoring ---\n{refactored}";
            });
}
