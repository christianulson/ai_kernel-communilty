using System.ComponentModel.Design;
using KrnlAI.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace KrnlAI.VisualStudio.Commands;

public sealed class AnalyzeErrorCommand
{
    public const int CommandId = 0x0300;
    public static readonly Guid CommandSet = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    private readonly AsyncPackage _package;

    private AnalyzeErrorCommand(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package;

        var cmdId = new CommandID(CommandSet, CommandId);
        var cmd = new OleMenuCommand(Execute, cmdId);
        cmd.BeforeQueryStatus += OnBeforeQueryStatus;
        commandService.AddCommand(cmd);
    }

    public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (((IServiceProvider)package).GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
        {
            _ = new AnalyzeErrorCommand(package, commandService);
        }
    }

    private static void OnBeforeQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (sender is OleMenuCommand cmd)
        {
            cmd.Visible = cmd.Enabled = HasSelectedError();
        }
    }

    private void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var error = GetSelectedError(
            ServiceProvider.GlobalProvider.GetService(typeof(SVsErrorList)) as IErrorList);
        if (error is null) return;

        var prompt = $"Analyze this build error and suggest a fix:\n\nError: {error.Value.Message}\nFile: {error.Value.File}\nLine: {error.Value.Line}";

        System.Windows.Clipboard.SetText(prompt);

        VsShellUtilities.ShowMessageBox(
            _package,
            $"Error copied to clipboard. Switch to Krnl-AI window and paste.\n\n{error.Value.Message}",
            "Krnl-AI - Error Analysis",
            OLEMSGICON.OLEMSGICON_INFO,
            OLEMSGBUTTON.OLEMSGBUTTON_OK,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    }

    private static bool HasSelectedError()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return GetSelectedError(
            ServiceProvider.GlobalProvider.GetService(typeof(SVsErrorList)) as IErrorList) is not null;
    }

    private static (string Message, string? File, int Line)? GetSelectedError(IErrorList? errorList)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            if (errorList?.TableControl is not IWpfTableControl wpfControl)
                return null;

            var selectedEntries = wpfControl.SelectedEntries;
            if (selectedEntries is null) return null;

            foreach (var entry in selectedEntries)
            {
                string? file = null;
                var line = 0;

                entry.TryGetValue(StandardTableKeyNames.DocumentName, out file);
                entry.TryGetValue(StandardTableKeyNames.Line, out line);

                if (entry.TryGetValue(StandardTableKeyNames.Text, out string? message) && !string.IsNullOrEmpty(message))
                {
                    return (message!, file, line);
                }
            }
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }

        return null;
    }
}
