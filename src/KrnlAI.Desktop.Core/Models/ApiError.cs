namespace KrnlAI.Desktop.Core.Models;

public abstract record ApiError(string Message, string? Code = null);

public sealed record KrnlAiAuthenticationError(string Message, string? Code = "AUTH_ERROR")
    : ApiError(Message, Code);

public sealed record KrnlAiValidationError(string Message, string? Code = "VALIDATION_ERROR", List<string>? Errors = null)
    : ApiError(Message, Code);

public sealed record KrnlAiNotFoundError(string Message, string? Code = "NOT_FOUND")
    : ApiError(Message, Code);

public sealed record KrnlAiServerError(string Message, string? Code = "SERVER_ERROR", int StatusCode = 500)
    : ApiError(Message, Code);

public static class ApiErrorClassifier
{
    public static ApiError Classify(int statusCode, string message)
    {
        return statusCode switch
        {
            401 => new KrnlAiAuthenticationError(message),
            404 => new KrnlAiNotFoundError(message),
            422 or 400 => new KrnlAiValidationError(message),
            >= 500 => new KrnlAiServerError(message, StatusCode: statusCode),
            _ => new KrnlAiServerError(message, StatusCode: statusCode)
        };
    }

    public static bool ShouldRetry(ApiError error)
    {
        return error switch
        {
            KrnlAiServerError => true,
            KrnlAiAuthenticationError => true,
            _ => false
        };
    }

    public static TimeSpan GetBackoffDelay(int attempt)
    {
        return TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 1000);
    }
}
