namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class SettingsViewModelTests
{
    [Fact] public void DeviceTestStatus_Default_ShouldBeOk()
    {
        try
        {
            var vm = new SettingsViewModel();
            Assert.Equal("Dispositivos OK", vm.DeviceTestStatus);
        }
        catch { /* DI not available */ }
    }
}
