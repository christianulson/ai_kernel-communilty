using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
namespace KrnlAI.Sidecar;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureSidecarPipeline(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint();
        app.UseExceptionHandling();
        app.UseCorrelationId();
        app.UseApiVersionHeader();
        app.UseSecurityHeaders();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuth();
        app.UseRateLimitHeaders();
        return app;
    }

    public static WebApplication UseExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(appError =>
        {
            appError.Run(async ctx =>
            {
                var reqId = ctx.Items["RequestId"]?.ToString() ?? "";
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new ErrorResponse("internal_error", "An unexpected error occurred.", reqId));
            });
        });
        return app;
    }

    public static WebApplication UseCorrelationId(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            var requestId = ctx.Request.Headers["X-Request-ID"].FirstOrDefault()
                          ?? ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                          ?? Guid.NewGuid().ToString();
            ctx.Items["RequestId"] = requestId;
            ctx.Response.Headers["X-Request-ID"] = requestId;

            var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
            using (logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
            {
                await next();
            }
        });
        return app;
    }

    public static WebApplication UseApiVersionHeader(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-API-Version"] = "1.0";
            await next();
        });
        return app;
    }

    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
        return app;
    }

    public static WebApplication UseAuth(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            if (!ctx.Request.Path.StartsWithSegments("/health"))
            {
                var options = ctx.RequestServices.GetRequiredService<IOptions<SidecarOptions>>();
                var authToken = options.Value.Auth.Token;
                if (!string.IsNullOrEmpty(authToken))
                {
                    var auth = ctx.Request.Headers["Authorization"].FirstOrDefault() ?? "";
                    var expected = $"Bearer {authToken}";
                    var authBytes = Encoding.UTF8.GetBytes(auth);
                    var expectedBytes = Encoding.UTF8.GetBytes(expected);
                    if (!CryptographicOperations.FixedTimeEquals(authBytes, expectedBytes))
                    {
                        var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Unauthorized access attempt to {Path} from {RemoteIp}", ctx.Request.Path, ctx.Connection.RemoteIpAddress);
                        ctx.Response.StatusCode = 401;
                        await ctx.Response.WriteAsJsonAsync(new ErrorResponse("unauthorized", null, ctx.Items["RequestId"]?.ToString()));
                        return;
                    }
                }
            }
            await next();
        });
        return app;
    }

    public static WebApplication UseRateLimitHeaders(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            await next();
            if (ctx.Response.StatusCode == 429 && !ctx.Response.Headers.ContainsKey("Retry-After"))
                ctx.Response.Headers["Retry-After"] = "60";
        });
        return app;
    }
}

public sealed record ErrorResponse(string Error, string? Message, string? RequestId);
