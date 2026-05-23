using System.Runtime.CompilerServices;

namespace KrnlAI.Desktop.Core.Services;

public sealed class KrnlLoggerConfig
{
    public string LogDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KrnlAI", "logs");
    public int MaxLogSizeBytes { get; init; } = 5 * 1024 * 1024;
    public string LogFileName { get; init; } = "krnl.log";
}

public static class KrnlLogger
{
    private static KrnlLoggerConfig _config = new();
    private static string _logDir = default!;
    private static string _logFile = default!;
    private static readonly object _lock = new();

    static KrnlLogger()
    {
        ApplyConfig(_config);
    }

    public static void Configure(KrnlLoggerConfig config)
    {
        _config = config;
        ApplyConfig(config);
    }

    public static void Write(
        Exception ex,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = filePath is not null ? Path.GetFileNameWithoutExtension(filePath) : "?";
        WriteLine($"[{fileName}.{memberName}:{lineNumber}] {FormatException(ex)}");
    }

    public static void Write(
        string message,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        var fileName = filePath is not null ? Path.GetFileNameWithoutExtension(filePath) : "?";
        WriteLine($"[{fileName}.{memberName}:{lineNumber}] {message}");
    }

    private static string FormatException(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        var depth = 0;
        for (var current = ex; current != null; current = current.InnerException, depth++)
        {
            if (depth > 0)
            {
                sb.AppendLine();
                sb.Append($"--- Inner Exception ({depth}) ---");
                sb.AppendLine();
            }
            sb.Append($"{current.GetType().Name}: {current.Message}");
            if (!string.IsNullOrEmpty(current.StackTrace))
            {
                sb.AppendLine();
                sb.Append(current.StackTrace);
            }
        }
        return sb.ToString();
    }

    private static void WriteLine(string line)
    {
        try
        {
            lock (_lock)
            {
                var entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {line}";
                File.AppendAllText(_logFile, entry + Environment.NewLine);
            }
        }
        catch
        {
            // Fail silently — logging must never break the app
        }
    }

    private static void ApplyConfig(KrnlLoggerConfig config)
    {
        _logDir = config.LogDirectory;
        _logFile = Path.Combine(_logDir, config.LogFileName);
        try
        {
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);
            RotateIfNeeded(config.MaxLogSizeBytes);
        }
        catch
        {
            // Fail silently — logging must never break the app
        }
    }

    private static void RotateIfNeeded(int maxSize)
    {
        if (!File.Exists(_logFile)) return;
        var info = new FileInfo(_logFile);
        if (info.Length < maxSize) return;

        var rotated = Path.Combine(_logDir, $"krnl.{DateTime.UtcNow:yyyyMMddHHmmss}.log");
        try
        {
            File.Move(_logFile, rotated);
        }
        catch
        {
            // Best-effort rotation
        }
    }

    public static string GetLogPath() => _logFile;
}
