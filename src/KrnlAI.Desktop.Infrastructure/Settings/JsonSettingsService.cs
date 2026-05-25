using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Infrastructure.Settings;

public class JsonSettingsService : ISettingsService
{
    private const string GatewayProxyDefaultUrl = "http://localhost:5235";

    private readonly string _settingsPath;
    private readonly string _settingsDir;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _fileLock = new();

    public JsonSettingsService()
    {
        _settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KrnlAI.Desktop"
        );

        Directory.CreateDirectory(_settingsDir);
        _settingsPath = Path.Combine(_settingsDir, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
    }

    public AppSettings LoadSettings()
    {
        lock (_fileLock)
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var encrypted = File.ReadAllBytes(_settingsPath);
                    var json = Decrypt(encrypted);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                    return NormalizeSettings(settings ?? new AppSettings());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("Failed to load settings: {0}", ex.Message);
            }

            return new AppSettings();
        }
    }

    private static AppSettings NormalizeSettings(AppSettings settings)
    {
        return settings;
    }

    public void SaveSettings(AppSettings settings)
    {
        lock (_fileLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                var encrypted = Encrypt(json);

                // Atomic write: write to temp file, then rename
                var tempPath = _settingsPath + ".tmp";
                File.WriteAllBytes(tempPath, encrypted);
                File.Delete(_settingsPath);
                File.Move(tempPath, _settingsPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("Failed to save settings: {0}", ex.Message);
            }
        }
    }

    private static byte[] Encrypt(string plaintext)
    {
        var data = Encoding.UTF8.GetBytes(plaintext);
        return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
    }

    private static string Decrypt(byte[] ciphertext)
    {
        var data = ProtectedData.Unprotect(ciphertext, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(data);
    }
}
