using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class LocalizationServiceTests
{
    [Fact]
    public void Constructor_WithoutEnv_ShouldDefaultToPtBr()
    {
        Environment.SetEnvironmentVariable("KRNL_LANG", null);

        var service = new LocalizationService();

        Assert.Equal("pt-BR", service.CurrentCulture);
    }

    [Fact]
    public void Constructor_WithEnvEn_ShouldUseEnglish()
    {
        Environment.SetEnvironmentVariable("KRNL_LANG", "en");
        try
        {
            var service = new LocalizationService();

            Assert.Equal("en", service.CurrentCulture);
        }
        finally
        {
            Environment.SetEnvironmentVariable("KRNL_LANG", null);
        }
    }

    [Fact]
    public void GetString_MissingKey_ShouldReturnBracketedKey()
    {
        var service = new LocalizationService();

        Assert.Equal("[missing.key]", service.GetString("missing.key"));
    }

    [Fact]
    public void SetCulture_ShouldRaiseCultureChanged()
    {
        var service = new LocalizationService();
        string? changed = null;
        service.CultureChanged += (_, c) => changed = c;

        service.SetCulture("en");

        Assert.Equal("en", changed);
    }

    [Fact]
    public void GetAvailableCultures_ShouldIncludePtBrAndEn()
    {
        var service = new LocalizationService();

        var cultures = service.GetAvailableCultures().ToList();

        Assert.Contains("pt-BR", cultures);
        Assert.Contains("en", cultures);
    }
}