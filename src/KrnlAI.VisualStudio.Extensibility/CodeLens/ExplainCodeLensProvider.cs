#pragma warning disable VSEXTPREVIEW_CODELENS // CodeLens editor APIs are preview in SDK 17.14 (stable in VS18); official opt-in
using KrnlAI.VisualStudio.Extensibility.Core.CodeLens;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using EditorCodeLens = Microsoft.VisualStudio.Extensibility.Editor.CodeLens;

namespace KrnlAI.VisualStudio.Extensibility.CodeLens;

/// <summary>
/// Provides the "Explain this code" CodeLens for classes, structs and methods.
/// </summary>
[VisualStudioContribution]
public sealed class ExplainCodeLensProvider : ICodeLensProvider
{
    private readonly IExplainService _explainService;

    /// <summary>Creates a new instance.</summary>
    public ExplainCodeLensProvider(IExplainService explainService)
    {
        _explainService = explainService;
    }

    /// <inheritdoc/>
    public TextViewExtensionConfiguration TextViewExtensionConfiguration => new()
    {
        AppliesTo = new[] { DocumentFilter.FromDocumentType(DocumentType.KnownValues.Text) }
    };

    /// <inheritdoc/>
    public CodeLensProviderConfiguration CodeLensProviderConfiguration => new("%KrnlAICodeLensDisplayName%");

    /// <inheritdoc/>
    public Task<EditorCodeLens?> TryCreateCodeLensAsync(
        CodeElement codeElement,
        CodeElementContext codeElementContext,
        CancellationToken cancellationToken)
    {
        if (!ExplainCodeLensPolicy.ShouldProvide(codeElement.Kind))
            return Task.FromResult<EditorCodeLens?>(null);

        return Task.FromResult<EditorCodeLens?>(new ExplainCodeLens(codeElement, _explainService));
    }
}