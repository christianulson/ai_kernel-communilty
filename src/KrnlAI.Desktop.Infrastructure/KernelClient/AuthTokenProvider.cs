namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public sealed class AuthTokenProvider
{
    private string? _token;
    private string? _refreshToken;
    private readonly object _lock = new();

    public string? Token
    {
        get { lock (_lock) return _token; }
        set { lock (_lock) _token = value; }
    }

    public string? RefreshToken
    {
        get { lock (_lock) return _refreshToken; }
        set { lock (_lock) _refreshToken = value; }
    }

    public void SetTokens(string? token, string? refreshToken)
    {
        lock (_lock)
        {
            _token = token;
            _refreshToken = refreshToken;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _token = null;
            _refreshToken = null;
        }
    }
}
