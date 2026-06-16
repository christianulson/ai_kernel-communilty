using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace KrnlAI.VisualStudio.CodeLens;

public sealed record KrnlAICodeLensEntry(
    string Title,
    string Description,
    string CommandName,
    string? CommandArg = null
) : CodeLensDetailEntryDescriptor(Title, Description, Description);

[Export(typeof(ICodeLensProvider))]
[Name("KrnlAI CodeLens Provider")]
[ContentType("text")]
public sealed class KrnlAICodeLensProvider : ICodeLensProvider
{
    private readonly Dictionary<CodeLensDescriptor, KrnlAIDataPoint> _dataPoints = new();

    public bool IsExperimental => false;

    public CodeLensDescriptor? CreateDescriptor(ITextBuffer textBuffer, CodeLensDescriptor parentDescriptor)
    {
        return null;
    }

    public IEnumerable<CodeLensDescriptor> GetDescriptors(ITextBuffer textBuffer)
    {
        var text = textBuffer.CurrentSnapshot.GetText();
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var descriptor = TryCreateDescriptor(textBuffer, line, i + 1);
            if (descriptor is not null)
                yield return descriptor;
        }
    }

    public ICodeLensDataPoint? TryCreateDataPoint(CodeLensDescriptor descriptor)
    {
        var entry = CreateEntry(descriptor);
        if (entry is null) return null;

        var dataPoint = new KrnlAIDataPoint(descriptor, entry);
        _dataPoints[descriptor] = dataPoint;
        return dataPoint;
    }

    private static CodeLensDescriptor? TryCreateDescriptor(ITextBuffer textBuffer, string line, int lineNumber)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("public ") && (trimmed.Contains(" class ") || trimmed.Contains(" struct ") || trimmed.Contains(" record ")))
        {
            return new CodeLensDescriptor(
                textBuffer,
                lineNumber,
                length: line.Length,
                trackingSpan: null,
                parent: null,
                provider: null
            );
        }

        if (trimmed.StartsWith("public ") && trimmed.Contains("(") && trimmed.Contains(")"))
        {
            return new CodeLensDescriptor(
                textBuffer,
                lineNumber,
                length: line.Length,
                trackingSpan: null,
                parent: null,
                provider: null
            );
        }

        return null;
    }

    private static KrnlAICodeLensEntry? CreateEntry(CodeLensDescriptor descriptor)
    {
        return new KrnlAICodeLensEntry(
            Title: "Krnl-AI",
            Description: "Explain this code",
            CommandName: "krnlai.explain",
            CommandArg: descriptor.TrackingSpan?.GetText(descriptor.TrackingSpan.TextBuffer.CurrentSnapshot)
        );
    }

    public void Dispose()
    {
        _dataPoints.Clear();
    }
}
