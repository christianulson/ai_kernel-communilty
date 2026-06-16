using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace KrnlAI.VisualStudio.Commands;

public abstract class KrnlAICommand
{
    protected readonly AsyncPackage Package;

    protected KrnlAICommand(AsyncPackage package, OleMenuCommandService commandService, int commandId, Guid commandSet)
    {
        Package = package;

        var cmdId = new CommandID(commandSet, commandId);
        var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
        cmd.BeforeQueryStatus += OnBeforeQueryStatus;
        commandService.AddCommand(cmd);
    }

    protected abstract void OnBeforeQueryStatus(object sender, EventArgs e);
    protected abstract Task ExecuteAsync(object sender, EventArgs e);

    protected static void ShowOutput(string title, string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            var outputWindow = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow is null) return;

            var guid = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
            outputWindow.CreatePane(ref guid, title, 1, 0);
            outputWindow.GetPane(ref guid, out var pane);
            pane?.OutputStringThreadSafe(message + Environment.NewLine);
            pane?.Activate();
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }

    protected static void ShowInfoBar(AsyncPackage package, string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            var infoBarFactory = package.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
            if (infoBarFactory is null) return;

            var shell = package.GetService(typeof(SVsShell)) as IVsShell;
            if (shell is null) return;

            shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
            if (obj is IVsInfoBarHost host)
            {
                var text = new InfoBarTextSpan(message);
                var spanSet = new[] { text };
                var actionSet = new[] { new InfoBarActionSpan("Dismiss") };
                var model = new InfoBarModel(spanSet, actionSet);
                var uiElement = infoBarFactory.CreateInfoBar(model);
                host.AddInfoBar(uiElement);
            }
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }
}
