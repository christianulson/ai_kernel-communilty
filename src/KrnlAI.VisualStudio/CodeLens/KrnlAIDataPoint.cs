#if AUTOCODE_ENABLE_CODELENS
using Microsoft.VisualStudio.Language.CodeLens;

namespace KrnlAI.VisualStudio.CodeLens;

public sealed class KrnlAIDataPoint : ICodeLensDataPoint
{
    public event EventHandler? Invalidated;

    public string Description { get; }
    public CodeLensDescriptor Descriptor { get; }
    public KrnlAICodeLensEntry Entry { get; }

    public KrnlAIDataPoint(CodeLensDescriptor descriptor, KrnlAICodeLensEntry entry)
    {
        Descriptor = descriptor;
        Entry = entry;
        Description = entry.Description;
    }

    public Task<CodeLensDetailsHeader> GetDetailsHeaderAsync(CancellationToken ct)
    {
        return Task.FromResult(new CodeLensDetailsHeader(
            EntryTitle: Entry.Title,
            entries: new[] { Entry }
        ));
    }

    public Task<IReadOnlyList<CodeLensDetailEntryDescriptor>?> GetDetailsContentAsync(CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<CodeLensDetailEntryDescriptor>?>(
            new[] { new CodeLensDetailEntryDescriptor(
                Entry,
                Value: Entry.Description,
                Tooltip: Entry.Description
            )}
        );
    }

    public void Invalidate()
    {
        Invalidated?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
    }
}
#endif
