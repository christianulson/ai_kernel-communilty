namespace KrnlAI.Sdk;

public class KrnlAIException : Exception
{
    public int? StatusCode { get; }

    public KrnlAIException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

public class KrnlAIAuthenticationException : KrnlAIException
{
    public KrnlAIAuthenticationException(string message = "Authentication failed", object? body = null)
        : base(message, 401) { }
}

public class KrnlAIRateLimitException : KrnlAIException
{
    public KrnlAIRateLimitException(string message = "Rate limit exceeded", object? body = null)
        : base(message, 429) { }
}

public class KrnlAIValidationException : KrnlAIException
{
    public KrnlAIValidationException(string message = "Validation failed", int statusCode = 400, object? body = null)
        : base(message, statusCode) { }
}

public class KrnlAIServerException : KrnlAIException
{
    public KrnlAIServerException(string message = "Internal server error", int statusCode = 500, object? body = null)
        : base(message, statusCode) { }
}
