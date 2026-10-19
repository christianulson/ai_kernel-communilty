namespace KrnlAI.VisualStudio.Extensibility.Core.CodeLens;

/// <summary>
/// Requests an AI explanation for a code element.
/// The concrete implementation (chat integration) is provided by the extension layer.
/// </summary>
public interface IExplainService
{
    /// <summary>Requests an explanation for the given code element.</summary>
    Task ExplainAsync(string elementIdentifier, CancellationToken cancellationToken);
}