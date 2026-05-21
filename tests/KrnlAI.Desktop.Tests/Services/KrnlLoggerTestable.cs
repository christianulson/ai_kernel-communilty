using System.IO;
using System.Runtime.CompilerServices;

namespace KrnlAI.Desktop.Tests.Services;

public static class KrnlLoggerTestable
{
    public static void Test(Action<KrnlLoggerTestWriter> testAction, string tempDir, int maxSize = 5 * 1024 * 1024)
    {
        var writer = new KrnlLoggerTestWriter(tempDir, maxSize);
        testAction(writer);
    }
}

public sealed class KrnlLoggerTestWriter
{
    private readonly string _logFile;
    private readonly string _logDir;
    private readonly int _maxSize;
    private readonly object _lock = new();

    public KrnlLoggerTestWriter(string tempDir, int maxSize = 5 * 1024 * 1024)
    {
        _logDir = tempDir;
        _logFile = Path.Combine(_logDir, "krnl.log");
        _maxSize = maxSize;
        if (!Directory.Exists(_logDir))
            Directory.CreateDirectory(_logDir);
    }

    public void Write(Exception ex, [CallerMemberName] string? caller = null)
    {
        WriteLine($"[{caller ?? "?"}] {ex.GetType().Name}: {ex.Message}");
    }

    public void Write(string message, [CallerMemberName] string? caller = null)
    {
        WriteLine($"[{caller ?? "?"}] {message}");
    }

    public string GetLogPath() => _logFile;

    private void WriteLine(string line)
    {
        lock (_lock)
        {
            var entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {line}";
            RotateIfNeeded();
            File.AppendAllText(_logFile, entry + Environment.NewLine);
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logFile)) return;
        var info = new FileInfo(_logFile);
        if (info.Length < _maxSize) return;
        var rotated = Path.Combine(_logDir, $"krnl.{DateTime.UtcNow:yyyyMMddHHmmss}.log");
        try { File.Move(_logFile, rotated); } catch { }
    }
}
