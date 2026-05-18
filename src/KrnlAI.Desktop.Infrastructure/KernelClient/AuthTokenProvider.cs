namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public sealed class AuthTokenProvider
{
    private string? _token;
    private readonly object _lock = new();

    public string? Token
    {
        get { lock (_lock) return _token; }
        set { lock (_lock) _token = value; }
    }
}
