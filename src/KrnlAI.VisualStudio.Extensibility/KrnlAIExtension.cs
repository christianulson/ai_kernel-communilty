using Microsoft.VisualStudio.Extensibility;

namespace KrnlAI.VisualStudio.Extensibility;

/// <summary>
/// Krnl-AI extension entry point on the new Visual Studio Extensibility model.
/// Target: VS 2022 17.14+ and VS 18 (homologation target).
/// </summary>
[VisualStudioContribution]
public sealed class KrnlAIExtension : Extension
{
    /// <summary>Extension identifier kept from the legacy VSIX (marketplace compatibility).</summary>
    public const string ExtensionId = "KrnlAI.VisualStudio.a6b3f8e1-2c4d-4e5f-8a9b-0c1d2e3f4a5b";

    /// <inheritdoc/>
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new ExtensionMetadata(
            id: ExtensionId,
            version: new Version(1, 0, 0),
            publisherName: "Krnl-AI",
            displayName: "Krnl-AI",
            description: "Krnl-AI developer assistant")
    };
}