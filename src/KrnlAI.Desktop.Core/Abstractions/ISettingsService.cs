using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface ISettingsService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
}