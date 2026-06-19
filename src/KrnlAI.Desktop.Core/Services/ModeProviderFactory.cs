namespace KrnlAI.Desktop.Core.Services;

public sealed class ModeProviderFactory(bool isLocal = false)
{
    public bool IsLocal { get; } = isLocal;
}
