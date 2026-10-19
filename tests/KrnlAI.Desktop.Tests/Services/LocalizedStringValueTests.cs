using System.ComponentModel;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class LocalizedStringValueTests
{
    [Fact]
    public void Value_ShouldReturnCurrentCultureString()
    {
        var service = new LocalizationService();
        var localized = new LocalizedStringValue(service, "status.connected");

        var value = localized.Value;

        Assert.False(string.IsNullOrEmpty(value));
    }

    [Fact]
    public void CultureChanged_ShouldNotifyValueProperty()
    {
        var service = new LocalizationService();
        var localized = new LocalizedStringValue(service, "status.connected");
        var notified = false;
        localized.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(LocalizedStringValue.Value)) notified = true; };

        service.SetCulture("en");

        Assert.True(notified);
    }
}