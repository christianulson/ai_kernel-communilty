using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class LocalizationServiceTests
{
    [Fact]
    public void GetString_ExistingKey_ShouldReturnValue()
    {
        var svc = new LocalizationService();
        var val = svc.GetString("login_button");
        Assert.False(string.IsNullOrEmpty(val));
    }

    [Fact]
    public void GetString_MissingKey_ShouldReturnKey()
    {
        var svc = new LocalizationService();
        Assert.Equal("[nonexistent_key]", svc.GetString("nonexistent_key"));
    }

    [Fact]
    public void GetAvailableCultures_ShouldReturnList()
    {
        var svc = new LocalizationService();
        var cultures = svc.GetAvailableCultures();
        Assert.NotEmpty(cultures);
    }

    [Fact]
    public void CurrentCulture_Default_ShouldBePtBr()
    {
        var svc = new LocalizationService();
        Assert.Equal("pt-BR", svc.CurrentCulture);
    }
}
