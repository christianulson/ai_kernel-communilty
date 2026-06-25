using System.Net;
using System.Runtime.CompilerServices;
using KrnlAI.Desktop.Core.Services;
using Refit;

namespace KrnlAI.Desktop.Infrastructure.KernelClient;

public static class SafeCall
{
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        T defaultOnError,
        [CallerMemberName] string? caller = null)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            WriteWithContext(ex, caller);
            return defaultOnError;
        }
    }

    public static async Task ExecuteAsync(
        Func<Task> action,
        [CallerMemberName] string? caller = null)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            WriteWithContext(ex, caller);
        }
    }

    private static void WriteWithContext(Exception ex, string? caller)
    {
        var ctx = FormatContext(ex);
        var tag = string.IsNullOrEmpty(ctx) ? "" : $" [{ctx}]";
        KrnlLogger.Write($"{ex.GetType().Name}: {ex.Message}{tag}{Environment.NewLine}{FormatException(ex)}",
            memberName: caller ?? "KernelClient");
    }

    private static string FormatException(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        var depth = 0;
        for (var current = ex; current != null; current = current.InnerException, depth++)
        {
            if (depth > 0) sb.AppendLine($"  --- Inner {depth}: {current.GetType().Name} ---");
            if (current.StackTrace != null) sb.AppendLine(current.StackTrace.Truncate(2000));
        }
        return sb.ToString();
    }

    private static string FormatContext(Exception ex)
    {
        var parts = new List<string>();

        if (ex is ApiException apiEx)
        {
            parts.Add($"HTTP {(HttpStatusCode)apiEx.StatusCode}");
            if (!string.IsNullOrEmpty(apiEx.Uri?.OriginalString))
                parts.Add(apiEx.Uri!.OriginalString);
            if (!string.IsNullOrEmpty(apiEx.Content))
                parts.Add($"body: {apiEx.Content.Truncate(500)}");
        }
        else if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode.HasValue)
                parts.Add($"HTTP {(int)httpEx.StatusCode.Value}");
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : "";
    }

    private static string Truncate(this string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";
}
