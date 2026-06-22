using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace KrnlAI.VisualStudio.Services;

public static class KrnlLogger
{
    private static readonly string _logDir;
    private static readonly string _logFile;
    private static readonly object _lock = new();
    private static readonly int MaxLogSize = 5 * 1024 * 1024;

    static KrnlLogger()
    {
        _logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KrnlAI", "logs");
        _logFile = Path.Combine(_logDir, "vs-krnl.log");
        try
        {
            if (!Directory.Exists(_logDir))
                Directory.CreateDirectory(_logDir);
            RotateIfNeeded();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[KrnlLogger] Init failed: {ex.Message}");
        }
    }

    public static void Write(
        Exception ex,
        [CallerMemberName] string? caller = null)
    {
        WriteLine($"[{caller ?? "?"}] {ex.GetType().Name}: {ex.Message}");
    }

    public static void Write(
        string message,
        [CallerMemberName] string? caller = null)
    {
        WriteLine($"[{caller ?? "?"}] {message}");
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
        catch (Exception ex)
        {
            Debug.WriteLine($"[KrnlLogger] Write failed: {ex.Message}");
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(_logFile)) return;
        var info = new FileInfo(_logFile);
        if (info.Length < MaxLogSize) return;
        var rotated = Path.Combine(_logDir, $"vs-krnl.{DateTime.UtcNow:yyyyMMddHHmmss}.log");
        try { File.Move(_logFile, rotated); }
        catch (Exception ex)
        {
            Debug.WriteLine($"[KrnlLogger] Rotate failed: {ex.Message}");
        }
    }

    public static string GetLogPath() => _logFile;
}
