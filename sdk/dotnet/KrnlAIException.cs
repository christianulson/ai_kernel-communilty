namespace KrnlAi.Sdk;

public class KrnlAiException : Exception
{
    public int? StatusCode { get; }

    public KrnlAiException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

public class KrnlAiAuthenticationException : KrnlAiException
{
    public KrnlAiAuthenticationException(string message = "Authentication failed", object? body = null)
        : base(message, 401) { }
}

public class KrnlAiRateLimitException : KrnlAiException
{
    public KrnlAiRateLimitException(string message = "Rate limit exceeded", object? body = null)
        : base(message, 429) { }
}

public class KrnlAiValidationException : KrnlAiException
{
    public KrnlAiValidationException(string message = "Validation failed", int statusCode = 400, object? body = null)
        : base(message, statusCode) { }
}

public class KrnlAiServerException : KrnlAiException
{
    public KrnlAiServerException(string message = "Internal server error", int statusCode = 500, object? body = null)
        : base(message, statusCode) { }
}
