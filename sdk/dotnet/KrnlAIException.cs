namespace KrnlAI.Sdk;

public class KrnlAIException(string message, int? statusCode = null, Exception? inner = null, object? body = null) : Exception(message, inner)
{
    public int? StatusCode { get; } = statusCode;

    public object? Body { get; } = body;
}

public class KrnlAIAuthenticationException(string message = "Authentication failed", object? body = null) : KrnlAIException(message, 401, body: body)
{
    
}

public class KrnlAIRateLimitException(string message = "Rate limit exceeded", object? body = null) : KrnlAIException(message, 429, body: body)
{
}

public class KrnlAIValidationException(string message = "Validation failed", int statusCode = 400, object? body = null) : KrnlAIException(message, statusCode, body: body)
{
}

public class KrnlAIServerException(string message = "Internal server error", int statusCode = 500, object? body = null) : KrnlAIException(message, statusCode, body: body)
{
}
