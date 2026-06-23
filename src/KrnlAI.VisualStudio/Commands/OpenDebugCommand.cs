using System.ComponentModel.Design;
using KrnlAI.VisualStudio.Services;
using KrnlAI.VisualStudio.ToolWindows;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.Commands;

public sealed class OpenDebugCommand
{
    public const int CommandId = 0x0400;
    public static readonly Guid CommandSet = new("7A8B9C0D-1E2F-3A4B-5C6D-7E8F9A0B1C2D");

    private readonly AsyncPackage _package;

    private OpenDebugCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package;
        var cmdId = new CommandID(CommandSet, CommandId);
        var cmd = new OleMenuCommand(Execute, cmdId);
        commandService.AddCommand(cmd);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (((IServiceProvider)package).GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
        {
            _ = new OpenDebugCommand(package, commandService);
        }
    }

    private void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            _ = _package.JoinableTaskFactory.RunAsync(async () =>
            {
                await _package.ShowToolWindowAsync(typeof(DebugToolWindow), 0, true, _package.DisposalToken);
            });
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }
}
