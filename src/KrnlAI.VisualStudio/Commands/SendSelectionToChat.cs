using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace KrnlAI.VisualStudio.Commands;

public sealed class SendSelectionToChat
{
    public const int CommandId = 0x0200;
    public static readonly Guid CommandSet = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    private readonly AsyncPackage _package;

    private SendSelectionToChat(AsyncPackage package, OleMenuCommandService commandService)
    {
        _package = package;

        var cmdId = new CommandID(CommandSet, CommandId);
        var cmd = new OleMenuCommand(Execute, cmdId);
        cmd.BeforeQueryStatus += OnBeforeQueryStatus;
        commandService.AddCommand(cmd);
    }

    public static async Task InitializeAsync(AsyncPackage package)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync();

        if (((IServiceProvider)package).GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
        {
            _ = new SendSelectionToChat(package, commandService);
        }
    }

    private static void OnBeforeQueryStatus(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (sender is OleMenuCommand cmd)
        {
            cmd.Visible = cmd.Enabled = HasSelection();
        }
    }

    private void Execute(object sender, EventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte?.ActiveDocument is EnvDTE.Document doc &&
                doc.Selection is EnvDTE.TextSelection sel &&
                !string.IsNullOrEmpty(sel.Text))
            {
                var code = sel.Text;
                var file = doc.FullName;
                var lang = doc.Language ?? "";

                var prompt = $"Analyze this {lang} from {System.IO.Path.GetFileName(file)}:\n\n```{lang}\n{code}\n```";
                System.Windows.Clipboard.SetText(prompt);

                VsShellUtilities.ShowMessageBox(
                    _package,
                    $"Code copied to clipboard. Switch to Krnl-AI window and paste.\n\nFile: {System.IO.Path.GetFileName(file)}\nLines: {sel.TopLine}-{sel.BottomLine}",
                    "Krnl-AI - Code Sent",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("Failed to send Visual Studio selection to chat: {0}", ex.Message);
        }
    }

    private static bool HasSelection()
    {
        try
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte?.ActiveDocument?.Selection is EnvDTE.TextSelection sel)
            {
                return !string.IsNullOrEmpty(sel.Text);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("Failed to inspect Visual Studio text selection: {0}", ex.Message);
        }

        return false;
    }
}
