namespace KrnlAI.Desktop.Tests.Models;

public class HotkeySettingTests
{
    [Fact]
    public void HotkeySetting_ShouldCreate()
    {
        var hk = new Core.Models.HotkeySetting("toggle_listening", "Toggle Escuta", "Ctrl+Shift", "K", true);
        Assert.Equal("toggle_listening", hk.Action);
        Assert.Equal("Ctrl+Shift", hk.Modifiers);
        Assert.Equal("K", hk.Key);
        Assert.True(hk.Enabled);
    }

    [Fact]
    public void HotkeySetting_ShouldSupportDisabled()
    {
        var hk = new Core.Models.HotkeySetting("test", "Test", "Ctrl", "T", false);
        Assert.False(hk.Enabled);
    }
}

public class HotkeySettingsTests
{
    [Fact]
    public void HotkeySettings_Default_ShouldHaveFourHotkeys()
    {
        var def = Core.Models.HotkeySettings.Default;
        Assert.Equal(4, def.Hotkeys.Count);
        Assert.Contains(def.Hotkeys, h => h.Action == "toggle_listening");
        Assert.Contains(def.Hotkeys, h => h.Action == "toggle_always_on_top");
        Assert.Contains(def.Hotkeys, h => h.Action == "show_window");
        Assert.Contains(def.Hotkeys, h => h.Action == "toggle_mute");
    }

    [Fact]
    public void HotkeySettings_Default_KeysAreCorrect()
    {
        var def = Core.Models.HotkeySettings.Default;
        var toggle = def.Hotkeys.First(h => h.Action == "toggle_listening");
        Assert.Equal("Ctrl+Shift", toggle.Modifiers);
        Assert.Equal("K", toggle.Key);
    }
}
