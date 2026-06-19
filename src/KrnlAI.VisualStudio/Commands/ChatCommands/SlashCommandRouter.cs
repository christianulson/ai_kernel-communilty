using KrnlAI.VisualStudio.Commands.ChatCommands.Handlers;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.Commands.ChatCommands;

public sealed class SlashCommandRouter
{
    private readonly Dictionary<string, SlashCommand> _commands;
    private readonly IKernelClientService _client;
    private readonly ISolutionContextService _context;
    private readonly IApplyEditService _applyEdit;
    private readonly IAgenticLoopService _agenticLoop;
    private readonly ITerminalService _terminal;
    private readonly IGitService _git;

    public SlashCommandRouter(
        IKernelClientService client,
        ISolutionContextService context,
        IApplyEditService applyEdit,
        IAgenticLoopService agenticLoop,
        ITerminalService? terminal = null,
        IGitService? git = null)
    {
        _client = client;
        _context = context;
        _applyEdit = applyEdit;
        _agenticLoop = agenticLoop;
        _terminal = terminal ?? new TerminalService();
        _git = git ?? new GitService();
        _commands = new Dictionary<string, SlashCommand>(StringComparer.OrdinalIgnoreCase);
        RegisterDefaultCommands();
    }

    public IReadOnlyDictionary<string, SlashCommand> Commands => _commands;

    public bool IsSlashCommand(string input) =>
        !string.IsNullOrEmpty(input) && input.StartsWith("/");

    private static string[] SplitCommand(string input)
    {
        var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1) return parts;
        return new[] { parts[0], string.Join(" ", parts.Skip(1)) };
    }

    public SlashCommand? Resolve(string input)
    {
        var parts = SplitCommand(input);
        if (parts.Length == 0) return null;
        var cmdName = parts[0].TrimStart('/').ToLowerInvariant();
        return _commands.TryGetValue(cmdName, out var cmd) ? cmd : null;
    }

    public async Task<string> ExecuteAsync(string input, CancellationToken ct)
    {
        var parts = SplitCommand(input);
        var cmdName = parts[0].TrimStart('/');
        var args = parts.Length > 1 ? parts[1] : "";

        if (!_commands.TryGetValue(cmdName, out var cmd))
            return $"Unknown command: /{cmdName}. Type /help for available commands.";

        if (cmd.IsVisible?.Invoke() == false)
            return $"Command /{cmdName} is not available in the current context.";

        return await cmd.Handler(args, ct);
    }

    public IReadOnlyList<SlashCommand> GetVisibleCommands()
    {
        return [.. _commands.Values.Where(c => c.IsVisible?.Invoke() ?? true)];
    }

    private void RegisterDefaultCommands()
    {
        Register(ExplainHandler.Create(_client, _context));
        Register(FixHandler.Create(_client, _context, _applyEdit));
        Register(TestHandler.Create(_client, _context));
        Register(RefactorHandler.Create(_client, _context, _applyEdit));
        Register(ReviewHandler.Create(_client, _context));
        Register(TaskHandler.Create(_client, _context, _agenticLoop));
        Register(HelpHandler.Create(this));

        Register(RunCommandHandler.Create(_terminal, _context));
        Register(BuildCommandHandler.Create(_terminal));
        Register(TestCmdHandler.Create(_terminal));
        Register(GitStatusHandler.Create(_git));
        Register(GitDiffHandler.Create(_git));
        Register(GitLogHandler.Create(_git));
        Register(GitBranchHandler.Create(_git));
        Register(GitCommitHandler.Create(_git));
        Register(GitReviewPrHandler.Create(_git, _client));
    }

    private void Register(SlashCommand cmd) => _commands[cmd.Name] = cmd;
}
