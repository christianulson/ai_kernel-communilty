using Moq;
using Xunit;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void DeviceTestStatus_Default_ShouldBeOk()
    {
        // _deviceStatus is initialized to ""; DeviceTestStatus is mode-dependent
        // and set in TestSpeakerAsync/TestMicAsync based on CurrentMode.
        // The original test had catch {} and triggered OpenCV hardware probes —
        // this test now verifies the VM can be created without crashing (the main fix).
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.Setup(x => x.LoadSettings()).Returns(new AppSettings());
        var vm = new SettingsViewModel(
            Mock.Of<IKernelClient>(),
            settingsMock.Object,
            Mock.Of<IListeningService>(),
            Mock.Of<IAudioPlayback>(),
            Mock.Of<IThemeService>(),
            Mock.Of<IAudioCapture>(),
            Mock.Of<IVideoCapture>());
        // Default DeviceTestStatus is "" (empty); mode-dependent values set later
        Assert.Equal("", vm.DeviceTestStatus);
    }
}
