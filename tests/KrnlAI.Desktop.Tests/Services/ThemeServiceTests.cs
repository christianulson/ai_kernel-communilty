using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.Tests.Services;

public class ThemeServiceTests
{
    [Fact]
    public void ThemeService_Default_ShouldBeDark()
    {
        var service = new ThemeService();
        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public void ThemeService_SetTheme_ShouldChangeTheme()
    {
        var service = new ThemeService();
        service.SetTheme("light");
        Assert.Equal("light", service.CurrentTheme);
    }

    [Fact]
    public void ThemeService_SetTheme_ShouldIgnoreInvalid()
    {
        var service = new ThemeService();
        service.SetTheme("invalid");
        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public void ThemeService_ToggleTheme_ShouldSwitch()
    {
        var service = new ThemeService();
        service.ToggleTheme();
        Assert.Equal("light", service.CurrentTheme);
        service.ToggleTheme();
        Assert.Equal("dark", service.CurrentTheme);
    }

    [Fact]
    public void ThemeService_ToggleTheme_ShouldRaiseEvent()
    {
        var service = new ThemeService();
        string? newTheme = null;
        service.ThemeChanged += (_, t) => newTheme = t;

        service.ToggleTheme();
        Assert.Equal("light", newTheme);
    }

    [Fact]
    public void ThemeService_GetAvailableThemes_ShouldReturnBoth()
    {
        var service = new ThemeService();
        var themes = service.GetAvailableThemes().ToList();
        Assert.Contains("dark", themes);
        Assert.Contains("light", themes);
    }
}
