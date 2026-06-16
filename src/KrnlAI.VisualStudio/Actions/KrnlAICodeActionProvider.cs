#if AUTOCODE_ENABLE_CODELENS
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace KrnlAI.VisualStudio.Actions;

[Export(typeof(ISuggestedActionSourceProvider))]
[Name("KrnlAI Code Action Provider")]
[ContentType("text")]
public sealed class KrnlAICodeActionProvider : ISuggestedActionSourceProvider
{
    public ISuggestedActionSource? CreateSuggestedActionSource(ITextView textView, ITextBuffer textBuffer)
    {
        return new KrnlAICodeActionSource(textView, textBuffer);
    }
}

public sealed class KrnlAICodeActionSource : ISuggestedActionSource
{
    private readonly ITextView _textView;
    private readonly ITextBuffer _textBuffer;

    public event EventHandler<EventArgs>? SuggestedActionsChanged { add { } remove { } }

    public KrnlAICodeActionSource(ITextView textView, ITextBuffer textBuffer)
    {
        _textView = textView;
        _textBuffer = textBuffer;
    }

    public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken ct)
    {
        return Task.FromResult(IsDiagnosticsRange(range) || HasSelection(range));
    }

    public Task<IEnumerable<SuggestedActionSet>> GetSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken ct)
    {
        var actions = new List<SuggestedActionSet>();

        if (IsDiagnosticsRange(range))
        {
            actions.Add(new SuggestedActionSet(
                categoryName: "Krnl-AI Diagnostics",
                actions: new ISuggestedAction[]
                {
                    new KrnlAISuggestedAction("Fix with Krnl-AI", "Analyze and fix this diagnostic", "krnlai.fix", _textBuffer, range)
                }
            ));
        }

        if (HasSelection(range))
        {
            actions.Add(new SuggestedActionSet(
                categoryName: "Krnl-AI",
                actions: new ISuggestedAction[]
                {
                    new KrnlAISuggestedAction("Explain with Krnl-AI", "Get AI explanation of selected code", "krnlai.explain", _textBuffer, range),
                    new KrnlAISuggestedAction("Generate Test", "Create unit test for selected code", "krnlai.test", _textBuffer, range)
                }
            ));
        }

        return Task.FromResult<IEnumerable<SuggestedActionSet>>(actions);
    }

    public void Dispose()
    {
    }

    private static bool IsDiagnosticsRange(SnapshotSpan range)
    {
        return false;
    }

    private static bool HasSelection(SnapshotSpan range)
    {
        return !range.IsEmpty;
    }
}

public sealed class KrnlAISuggestedAction : ISuggestedAction
{
    private readonly string _displayText;
    private readonly string _description;
    private readonly string _commandName;
    private readonly ITextBuffer _textBuffer;
    private readonly SnapshotSpan _range;

    public bool HasActionSets => false;
    public bool HasPreview => false;
    public string DisplayText => _displayText;

    public event EventHandler<EventArgs>? ActionCompleted { add { } remove { } }

    public KrnlAISuggestedAction(
        string displayText,
        string description,
        string commandName,
        ITextBuffer textBuffer,
        SnapshotSpan range)
    {
        _displayText = displayText;
        _description = description;
        _commandName = commandName;
        _textBuffer = textBuffer;
        _range = range;
    }

    public Task<SuggestedActionSet?> GetActionSetsAsync(CancellationToken ct)
    {
        return Task.FromResult<SuggestedActionSet?>(null);
    }

    public Task<object?> GetPreviewAsync(CancellationToken ct)
    {
        return Task.FromResult<object?>(null);
    }

    public Task InvokeAsync(CancellationToken ct)
    {
        var text = _range.GetText();
        var prompt = _commandName switch
        {
            "krnlai.explain" => $"Explain this code:\n\n```\n{text}\n```",
            "krnlai.fix" => $"Fix this code:\n\n```\n{text}\n```",
            "krnlai.test" => $"Generate a unit test for this code:\n\n```\n{text}\n```",
            _ => text
        };

        try
        {
            System.Windows.Clipboard.SetText(prompt);
            VsShellUtilities.ShowMessageBox(
                ServiceProvider.GlobalProvider,
                $"Copied to clipboard. Switch to Krnl-AI window and paste.",
                "Krnl-AI",
                Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_INFO,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
        catch { }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    public bool TryGetTelemetryId(out Guid telemetryId)
    {
        telemetryId = Guid.Empty;
        return false;
    }
}
#endif
