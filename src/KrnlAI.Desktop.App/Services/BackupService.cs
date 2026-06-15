using System.IO;
using System.IO.Compression;

namespace KrnlAI.Desktop.App.Services;

public sealed class BackupService
{
    public Task BackupAsync(string targetPath)
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KrnlAI");
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        if (File.Exists(targetPath)) File.Delete(targetPath);
        return Task.Run(() =>
        {
            using var archive = ZipFile.Open(targetPath, ZipArchiveMode.Create);
            ArchiveFiles(archive, baseDir);
        });
    }

    public Task RestoreAsync(string sourcePath)
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KrnlAI");
        return Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(sourcePath);
            foreach (var entry in archive.Entries)
            {
                var targetFile = Path.Combine(baseDir, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                entry.ExtractToFile(targetFile, overwrite: true);
            }
        });
    }

    private static void ArchiveFiles(ZipArchive archive, string baseDir)
    {
        var settingsPath = Path.Combine(baseDir, "settings.json");
        if (File.Exists(settingsPath))
            archive.CreateEntryFromFile(settingsPath, "settings.json");
        var sessionsPath = Path.Combine(baseDir, "sessions.json");
        if (File.Exists(sessionsPath))
            archive.CreateEntryFromFile(sessionsPath, "sessions.json");
        var logDir = Path.Combine(baseDir, "logs");
        if (Directory.Exists(logDir))
            foreach (var log in Directory.GetFiles(logDir, "*.log").OrderByDescending(f => f).Take(3))
                archive.CreateEntryFromFile(log, $"logs/{Path.GetFileName(log)}");
    }
}
