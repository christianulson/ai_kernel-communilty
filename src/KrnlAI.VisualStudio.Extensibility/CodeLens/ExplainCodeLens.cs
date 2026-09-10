#pragma warning disable VSEXTPREVIEW_CODELENS // CodeLens editor APIs are preview in SDK 17.14 (stable in VS18); official opt-in
using KrnlAI.VisualStudio.Extensibility.Core.CodeLens;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using EditorCodeLens = Microsoft.VisualStudio.Extensibility.Editor.CodeLens;

namespace KrnlAI.VisualStudio.Extensibility.CodeLens;

/// <summary>
/// Invokable CodeLens that requests an AI explanation for the element it is attached to.
/// </summary>
public sealed class ExplainCodeLens : InvokableCodeLens
{
    private readonly CodeElement _element;
    private readonly IExplainService _explainService;

    /// <summary>Creates a new instance.</summary>
    public ExplainCodeLens(CodeElement element, IExplainService explainService)
    {
        _element = element;
        _explainService = explainService;
    }

    /// <inheritdoc/>
    public override Task<CodeLensLabel> GetLabelAsync(
        CodeElementContext context,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new CodeLensLabel
        {
            Text = ExplainCodeLensPolicy.Label,
            Tooltip = "%KrnlAICodeLensDescription%"
        });
    }

    /// <inheritdoc/>
    public override Task ExecuteAsync(
        CodeElementContext context,
        IClientContext clientContext,
        CancellationToken cancellationToken)
    {
        return _explainService.ExplainAsync(
            _element.UniqueIdentifier ?? _element.Description ?? string.Empty,
            cancellationToken);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        // Nothing to release; kept for symmetry with the abstract base class.
    }
}