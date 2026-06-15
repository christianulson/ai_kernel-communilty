using System.Diagnostics;
using System.IO;
using System.Text;

namespace KrnlAI.Desktop.App.Services;

public sealed class CliBridgeService
{
    private string? FindCliPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\..\\..\\..\\Community\\src\\KrnlAI.Cli\\bin\\Debug\\net10.0\\KrnlAI.Cli.exe"),
            Path.Combine(AppContext.BaseDirectory, "..\\KrnlAI.Cli.exe"),
            "KrnlAI.Cli.exe"
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public async Task<string> ExecuteAsync(string command, int timeoutMs = 30000)
    {
        var cliPath = FindCliPath();
        if (cliPath == null) return "CLI não encontrado. Compile KrnlAI.Cli primeiro.";

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);

            process.OutputDataReceived += (_, e) => { if (e.Data == null) outputWaitHandle.Set(); else outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data == null) errorWaitHandle.Set(); else errorBuilder.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (process.WaitForExit(timeoutMs))
            {
                outputWaitHandle.WaitOne(1000);
                errorWaitHandle.WaitOne(1000);

                var output = outputBuilder.ToString().Trim();
                var error = errorBuilder.ToString().Trim();
                return string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
            }
            else
            {
                process.Kill();
                return "Comando excedeu o tempo limite.";
            }
        }
        catch (Exception ex)
        {
            return $"Erro ao executar CLI: {ex.Message}";
        }
    }
}
