using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace KrnlAI.VisualStudio.Services;

public sealed class ApplyEditService(
    IVsOperationTracker? debugTracker = null) : IApplyEditService
{
    private readonly IVsOperationTracker _debugTracker = debugTracker ?? new VsOperationTracker();
    private readonly Stack<string> _undoStack = new();

    public async Task<bool> PreviewAndApplyAsync(string diff, CancellationToken ct)
    {
        using var op = _debugTracker.Start("apply_edit.preview_apply");
        var preview = await PreviewDiffAsync(diff, ct);
        if (!preview.Approved || preview.Diff is null)
        {
            op.SetResult("Rejected");
            return false;
        }

        var result = await ApplyAsync(preview.Diff, ct);
        op.SetResult(result ? "Applied" : "Failed");
        return result;
    }

    public async Task<ApplyEditResult> PreviewDiffAsync(string diff, CancellationToken ct)
    {
        using var op = _debugTracker.Start("apply_edit.preview");
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

        var approved = VsShellUtilities.ShowMessageBox(
            ServiceProvider.GlobalProvider,
            $"Krnl-AI wants to apply these changes:\n\n{diff}\n\nApply changes?",
            "Krnl-AI - Edit Approval",
            OLEMSGICON.OLEMSGICON_QUERY,
            OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST) == 6;

        op.SetResult(approved ? "Approved" : "Rejected");
        return new ApplyEditResult(approved, diff, approved ? null : "Cancelled by user");
    }

    public async Task<bool> ApplyAsync(string diff, CancellationToken ct)
    {
        using var op = _debugTracker.Start("apply_edit.apply");
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

        try
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte?.ActiveDocument is not EnvDTE.Document doc)
            {
                op.SetResult("No active document");
                return false;
            }

            var textDoc = doc.Object("TextDocument") as EnvDTE.TextDocument;
            if (textDoc is null)
            {
                op.SetResult("No text document");
                return false;
            }

            var startPoint = textDoc.StartPoint.CreateEditPoint();
            var endPoint = textDoc.EndPoint.CreateEditPoint();
            _undoStack.Push(doc.FullName);

            startPoint.Delete(endPoint);

            var lines = diff.Split(['\n'], StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!line.StartsWith("+") && !line.StartsWith("-"))
                    startPoint.Insert(line + "\n");
            }

            doc.Save(doc.FullName);
            op.SetResult("Applied");
            return true;
        }
        catch (Exception ex)
        {
            op.SetError(ex.Message);
            KrnlLogger.Write(ex);
            return false;
        }
    }

    public async Task UndoAsync()
    {
        using var op = _debugTracker.Start("apply_edit.undo");
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        if (_undoStack.Count > 0)
        {
            _undoStack.Pop();
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            dte?.ActiveDocument?.Undo();
            op.SetResult("Undone");
        }
        else
        {
            op.SetResult("Nothing to undo");
        }
    }
}
