using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace KrnlAI.VisualStudio.Services;

public sealed class ApplyEditService : IApplyEditService
{
    private readonly Stack<string> _undoStack = new();

    public async Task<bool> PreviewAndApplyAsync(string diff, CancellationToken ct)
    {
        var preview = await PreviewDiffAsync(diff, ct);
        if (!preview.Approved || preview.Diff is null)
            return false;

        return await ApplyAsync(preview.Diff, ct);
    }

    public async Task<ApplyEditResult> PreviewDiffAsync(string diff, CancellationToken ct)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

        var approved = VsShellUtilities.ShowMessageBox(
            ServiceProvider.GlobalProvider,
            $"Krnl-AI wants to apply these changes:\n\n{diff}\n\nApply changes?",
            "Krnl-AI - Edit Approval",
            OLEMSGICON.OLEMSGICON_QUERY,
            OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST) == 6;

        return new ApplyEditResult(approved, diff, approved ? null : "Cancelled by user");
    }

    public async Task<bool> ApplyAsync(string diff, CancellationToken ct)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

        try
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte?.ActiveDocument is not EnvDTE.Document doc)
                return false;

            var textDoc = doc.Object("TextDocument") as EnvDTE.TextDocument;
            if (textDoc is null) return false;

            var startPoint = textDoc.StartPoint.CreateEditPoint();
            var endPoint = textDoc.EndPoint.CreateEditPoint();
            _undoStack.Push(doc.FullName);

            startPoint.Delete(endPoint);

            var lines = diff.Split(new[] { '\n' }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!line.StartsWith("+") && !line.StartsWith("-"))
                    startPoint.Insert(line + "\n");
            }

            doc.Save(doc.FullName);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning("ApplyEdit failed: {0}", ex.Message);
            return false;
        }
    }

    public async Task UndoAsync()
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (_undoStack.Count > 0)
        {
            _undoStack.Pop();
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            dte?.ActiveDocument?.Undo();
        }
    }
}
