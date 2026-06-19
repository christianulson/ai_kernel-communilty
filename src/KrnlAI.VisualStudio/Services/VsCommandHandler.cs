using KrnlAI.VisualStudio.Commands.ChatCommands;

namespace KrnlAI.VisualStudio.Services;

public interface IVsCommandHandler
{
    Task<string> ExecuteCommandAsync(string commandName, string args, CancellationToken ct = default);
    IReadOnlyDictionary<string, SlashCommand> Commands { get; }
}

public sealed class VsCommandHandler(
    IKernelClientService client,
    ISolutionContextService context,
    IApplyEditService applyEdit,
    IAgenticLoopService agenticLoop,
    ITerminalService? terminal = null,
    IGitService? git = null) : IVsCommandHandler
{
    private readonly SlashCommandRouter _router = new(client, context, applyEdit, agenticLoop, terminal, git);

    public IReadOnlyDictionary<string, SlashCommand> Commands => _router.Commands;

    public Task<string> ExecuteCommandAsync(string commandName, string args, CancellationToken ct = default)
    {
        var input = string.IsNullOrEmpty(args)
            ? $"/{commandName}"
            : $"/{commandName} {args}";

        return _router.ExecuteAsync(input, ct);
    }
}
