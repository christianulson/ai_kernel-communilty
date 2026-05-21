using System.IO;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.Tests.Services;
using Xunit;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class KrnlLoggerTests : IDisposable
{
    private readonly string _tempDir;

    public KrnlLoggerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "KrnlAI_LoggerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); }
        catch { }
    }

    [Fact]
    public void Write_WithException_ShouldCreateLogFile()
    {
        var logPath = Path.Combine(_tempDir, "test.log");
        KrnlLoggerTestable.Test((writer) =>
        {
            writer.Write(new InvalidOperationException("test error"));

            Assert.True(File.Exists(writer.GetLogPath()));
            var content = File.ReadAllText(writer.GetLogPath());
            Assert.Contains("InvalidOperationException", content);
            Assert.Contains("test error", content);
        }, _tempDir);
    }

    [Fact]
    public void Write_WithMessage_ShouldContainTimestamp()
    {
        KrnlLoggerTestable.Test((writer) =>
        {
            writer.Write("hello world");

            var content = File.ReadAllText(writer.GetLogPath());
            Assert.Contains("hello world", content);
            Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", content);
        }, _tempDir);
    }

    [Fact]
    public void Write_MultipleCalls_ShouldAppend()
    {
        KrnlLoggerTestable.Test((writer) =>
        {
            writer.Write("line1");
            writer.Write("line2");

            var content = File.ReadAllText(writer.GetLogPath());
            var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
        }, _tempDir);
    }

    [Fact]
    public void Rotate_WhenExceedsMaxSize_ShouldCreateNewFile()
    {
        KrnlLoggerTestable.Test((writer) =>
        {
            var bigLine = new string('X', 2 * 1024 * 1024);
            writer.Write(bigLine);
            writer.Write(bigLine);
            writer.Write(bigLine);

            var dir = Path.GetDirectoryName(writer.GetLogPath())!;
            var files = Directory.GetFiles(dir, "krnl.*.log");
            Assert.NotEmpty(files);
        }, _tempDir, maxSize: 1 * 1024 * 1024);
    }

    [Fact]
    public void Write_ConcurrentAccess_ShouldNotThrow()
    {
        KrnlLoggerTestable.Test((writer) =>
        {
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            Parallel.For(0, 20, i =>
            {
                try { writer.Write($"message-{i}"); }
                catch (Exception ex) { exceptions.Add(ex); }
            });

            Assert.Empty(exceptions);
            var content = File.ReadAllText(writer.GetLogPath());
            var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(20, lines.Length);
        }, _tempDir);
    }

    [Fact]
    public void Write_ToReadOnlyDirectory_ShouldNotThrow()
    {
        var readOnlyDir = Path.Combine(_tempDir, "readonly");
        Directory.CreateDirectory(readOnlyDir);
        try
        {
            var dirInfo = new DirectoryInfo(readOnlyDir);
            dirInfo.Attributes |= FileAttributes.ReadOnly;

            KrnlLoggerTestable.Test((writer) =>
            {
                var ex = Record.Exception(() => writer.Write("test"));
                Assert.Null(ex);
            }, readOnlyDir);
        }
        finally
        {
            new DirectoryInfo(readOnlyDir) { Attributes = FileAttributes.Normal };
        }
    }
}
