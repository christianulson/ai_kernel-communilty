namespace KrnlAI.Desktop.Core.Models;

public record HotkeySetting(
    string Action,
    string DisplayName,
    string Modifiers,
    string Key,
    bool Enabled = true
);

public record HotkeySettings(
    List<HotkeySetting> Hotkeys
)
{
    public static HotkeySettings Default => new(
    [
        new("toggle_listening", "Toggle Escuta Contínua", "Ctrl+Shift", "K", true),
        new("toggle_always_on_top", "Toggle Sempre no Topo", "Ctrl+Shift", "T", true),
        new("show_window", "Mostrar Janela", "Ctrl+Shift", "W", true),
        new("toggle_mute", "Toggle Mudo", "Ctrl+Shift", "M", true),
    ]);
}