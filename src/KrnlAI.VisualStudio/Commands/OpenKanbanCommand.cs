using System.ComponentModel.Design;
using KrnlAI.VisualStudio.Services;
using KrnlAI.VisualStudio.ToolWindows.Kanban;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace KrnlAI.VisualStudio.Commands;

public sealed class OpenKanbanCommand
{
    public const int CommandId = 0x0300;
    public static readonly Guid CommandSet = Guid.Parse("B2C3D4E5-F6A7-8901-BCDE-F12345678902");

    private readonly AsyncPackage _package;

    private OpenKanbanCommand(AsyncPackage package, OleMenuCommandService commandService)
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
            _ = new OpenKanbanCommand(package, commandService);
        }
    }

    private void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            _ = _package.JoinableTaskFactory.RunAsync(async () =>
            {
                await _package.ShowToolWindowAsync(typeof(KanbanToolWindow), 0, true, _package.DisposalToken);
            });
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }
}
