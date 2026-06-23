using KrnlAI.Desktop.Infrastructure.Settings;

namespace KrnlAI.Desktop.Tests.Services;

public class JsonSettingsServiceTests
{
    [Fact]
    public void LoadSettings_ShouldReturnNonNull()
    {
        Assert.NotNull(new JsonSettingsService().LoadSettings());
    }

    [Fact]
    public void SaveSettings_ShouldNotThrow()
    {
        var ex = Record.Exception(() => new JsonSettingsService().SaveSettings(new()));
        Assert.Null(ex);
    }
}
