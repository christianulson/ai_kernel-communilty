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

    public async Task<bool> ApplyAsync(string content, CancellationToken ct)
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

            // Extract code from markdown blocks if present, or use content as-is
            var code = content;
            var codeMatch = System.Text.RegularExpressions.Regex.Match(content,
                @"```(?:\w+)?\s*\n([\s\S]*?)```");
            if (codeMatch.Success)
                code = codeMatch.Groups[1].Value.Trim();

            // Apply unified diff if content looks like one
            if (content.Contains("\n--- ") && content.Contains("\n+++ ") && content.Contains("\n@@"))
            {
                var originalText = textDoc.StartPoint.CreateEditPoint()
                    .GetText(textDoc.EndPoint.CreateEditPoint());
                var patched = ApplyUnifiedDiff(originalText, content);
                if (patched is not null)
                    code = patched;
                else
                    op.SetResult("Diff parse failed, using raw content");
            }

            _undoStack.Push(doc.FullName);
            var editPoint = textDoc.StartPoint.CreateEditPoint();
            var endPoint = textDoc.EndPoint.CreateEditPoint();
            editPoint.Delete(endPoint);
            editPoint.Insert(code);

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

    private static string? ApplyUnifiedDiff(string originalText, string diff)
    {
        try
        {
            var originalLines = originalText
                .Split(['\n'], StringSplitOptions.None)
                .Select((line, i) => new { Index = i, Text = line })
                .ToList();

            var result = new List<string>(originalLines.Select(l => l.Text));
            var diffLines = diff.Split(['\n'], StringSplitOptions.None);
            var i = 0;

            while (i < diffLines.Length)
            {
                var line = diffLines[i];
                if (line.StartsWith("@@"))
                {
                    // Parse hunk header: @@ -start,count +start,count @@
                    var match = System.Text.RegularExpressions.Regex.Match(line,
                        @"@@\s*-(\d+)(?:,\d+)?\s*\+(\d+)(?:,\d+)?\s*@@");
                    if (!match.Success) { i++; continue; }

                    var oldStart = int.Parse(match.Groups[1].Value) - 1;
                    i++;

                    // Apply hunk
                    var hunkLines = new List<string>();
                    while (i < diffLines.Length && !diffLines[i].StartsWith("@@") && !diffLines[i].StartsWith("diff") && !diffLines[i].StartsWith("---") && !diffLines[i].StartsWith("+++"))
                    {
                        hunkLines.Add(diffLines[i]);
                        i++;
                    }

                    // Apply hunk to result at oldStart position
                    var insertPos = Math.Min(oldStart, result.Count);
                    var linesToRemove = 0;
                    var linesToInsert = new List<string>();
                    var contextMatch = 0;

                    foreach (var hl in hunkLines)
                    {
                        if (hl.StartsWith("-"))
                        {
                            linesToRemove++;
                            contextMatch = 0;
                        }
                        else if (hl.StartsWith("+"))
                        {
                            linesToInsert.Add(hl.Substring(1));
                            contextMatch = 0;
                        }
                        else
                        {
                            var text = hl.StartsWith(" ") ? hl.Substring(1) : hl;
                            if (linesToRemove == 0 && linesToInsert.Count == 0)
                            {
                                // Context before changes — find matching position
                                insertPos = oldStart + contextMatch;
                            }
                            contextMatch++;
                        }
                    }

                    // Apply: remove old lines, insert new ones
                    if (linesToRemove > 0 && insertPos < result.Count)
                        result.RemoveRange(insertPos, Math.Min(linesToRemove, result.Count - insertPos));
                    if (linesToInsert.Count > 0)
                        result.InsertRange(insertPos, linesToInsert);
                }
                else
                {
                    i++;
                }
            }

            return string.Join("\n", result);
        }
        catch
        {
            return null; // Fall back to raw content
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
