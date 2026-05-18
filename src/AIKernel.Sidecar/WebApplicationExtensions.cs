using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace KrnlAI.Sidecar;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureSidecarPipeline(this WebApplication app)
    {
        // Global exception handler
        app.MapPrometheusScrapingEndpoint();

        app.UseExceptionHandler(appError =>
        {
            appError.Run(async ctx =>
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new { error = "internal_server_error", message = "An unexpected error occurred." });
            });
        });

        app.UseCors();
        app.UseRateLimiter();

        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });

        app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Path != "/health")
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
                        await ctx.Response.WriteAsJsonAsync(new { error = "unauthorized" });
                        return;
                    }
                }
            }
            await next();
        });

        return app;
    }
}
