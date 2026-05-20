using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

public static class FixHandler
{
    public static SlashCommand Create(IKernelClientService client, ISolutionContextService context, IApplyEditService applyEdit) =>
        new("fix", "Fix issues in the current code selection",
            async (args, ct) =>
            {
                var selection = context.GetActiveSelection();
                var code = selection?.SelectedText ?? args;
                if (string.IsNullOrWhiteSpace(code))
                    return "No code selected. Select code with issues or provide code as argument.";

                var prompt = $"Fix any issues in this code and return the corrected version:\n\n```{selection?.Language ?? ""}\n{code}\n```";
                var result = await client.RunAgentAsync(prompt, ct: ct);
                var fixedCode = result?.Summary ?? result?.Status;
                if (string.IsNullOrWhiteSpace(fixedCode))
                    return "No fix available.";

                var diff = $"- Original:\n{code}\n+ Fixed:\n{fixedCode}";
                var applied = await applyEdit.PreviewAndApplyAsync(diff, ct);
                return applied
                    ? "Fix applied. Review the changes in the editor."
                    : $"Fix cancelled by user.\n\n--- Suggested fix ---\n{fixedCode}";
            });
}
