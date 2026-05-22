using KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.Core.Services;

public sealed class ModeProviderFactory
{
    public bool IsLocal { get; }

    public ModeProviderFactory(bool isLocal = false)
    {
        IsLocal = isLocal;
    }
}
