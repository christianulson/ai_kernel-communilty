using System.Runtime.CompilerServices;
using KrnlAI.Desktop.Core.Services;

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
            return await action();
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex, memberName: caller ?? "KernelClient");
            return defaultOnError;
        }
    }

    public static async Task ExecuteAsync(
        Func<Task> action,
        [CallerMemberName] string? caller = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex, memberName: caller ?? "KernelClient");
        }
    }
}
