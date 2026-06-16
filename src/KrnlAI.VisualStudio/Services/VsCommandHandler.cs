using System.Text.Json;
using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;

namespace KrnlAI.VisualStudio.Services;

public interface IVsCommandHandler
{
    Task<string> ExecuteCommandAsync(string commandName, string args, CancellationToken ct = default);
    IReadOnlyDictionary<string, SlashCommand> Commands { get; }
}

public sealed class VsCommandHandler : IVsCommandHandler
{
    private readonly SlashCommandRouter _router;

    public VsCommandHandler(
        IKernelClientService client,
        ISolutionContextService context,
        IApplyEditService applyEdit,
        IAgenticLoopService agenticLoop,
        ITerminalService? terminal = null,
        IGitService? git = null)
    {
        _router = new SlashCommandRouter(client, context, applyEdit, agenticLoop, terminal, git);
    }

    public IReadOnlyDictionary<string, SlashCommand> Commands => _router.Commands;

    public Task<string> ExecuteCommandAsync(string commandName, string args, CancellationToken ct = default)
    {
        var input = string.IsNullOrEmpty(args)
            ? $"/{commandName}"
            : $"/{commandName} {args}";

        return _router.ExecuteAsync(input, ct);
    }
}
