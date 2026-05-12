namespace AiKernel.Sdk;

public class AiKernelException : Exception
{
    public int? StatusCode { get; }

    public AiKernelException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}

public class AiKernelAuthenticationException : AiKernelException
{
    public AiKernelAuthenticationException(string message = "Authentication failed", object? body = null)
        : base(message, 401) { }
}

public class AiKernelRateLimitException : AiKernelException
{
    public AiKernelRateLimitException(string message = "Rate limit exceeded", object? body = null)
        : base(message, 429) { }
}

public class AiKernelValidationException : AiKernelException
{
    public AiKernelValidationException(string message = "Validation failed", int statusCode = 400, object? body = null)
        : base(message, statusCode) { }
}

public class AiKernelServerException : AiKernelException
{
    public AiKernelServerException(string message = "Internal server error", int statusCode = 500, object? body = null)
        : base(message, statusCode) { }
}
