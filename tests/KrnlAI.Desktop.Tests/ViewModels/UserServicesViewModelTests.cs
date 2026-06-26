using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class UserServicesViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new UserServicesViewModel();

        Assert.False(vm.IsLoading);
        Assert.Empty(vm.Services);
        Assert.Empty(vm.StatusMessage);
    }

    [Fact]
    public void Constructor_Di_ShouldInitializeProperties()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new UserServicesViewModel(kernelClient.Object);

        Assert.False(vm.IsLoading);
        Assert.Empty(vm.Services);
        Assert.Empty(vm.StatusMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenApiReturnsServices_ShouldPopulateList()
    {
        var kernelClient = new Mock<IKernelClient>();
        var services = new List<UserServiceInfo>
        {
            new("github", true, true, DateTimeOffset.UtcNow.AddDays(-1)),
            new("slack", true, false, null),
        };
        kernelClient.Setup(k => k.GetUserServicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(services);

        var vm = new UserServicesViewModel(kernelClient.Object);

        await vm.LoadAsync();

        Assert.Equal(2, vm.Services.Count);
        Assert.Equal("github", vm.Services[0].ServiceType);
        Assert.True(vm.Services[0].Enabled);
        Assert.Equal("slack", vm.Services[1].ServiceType);
        Assert.False(vm.Services[1].Enabled);
    }

    [Fact]
    public async Task LoadAsync_WhenApiThrows_ShouldUseDemoData()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetUserServicesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("api down"));

        var vm = new UserServicesViewModel(kernelClient.Object);

        await vm.LoadAsync();

        Assert.NotEmpty(vm.Services);
        Assert.Contains(vm.Services, s => s.ServiceType == "demo");
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetIsLoadingDuringExecution()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<List<UserServiceInfo>>();
        kernelClient.Setup(k => k.GetUserServicesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => tcs.Task);

        var vm = new UserServicesViewModel(kernelClient.Object);

        var loadTask = vm.LoadAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult(new List<UserServiceInfo>());
        await loadTask;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void ToggleService_WhenServiceFound_ShouldToggleEnabled()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new UserServicesViewModel(kernelClient.Object);
        var service = new UserServiceInfo("github", true, true, DateTimeOffset.UtcNow);
        vm.Services.Add(service);

        vm.ToggleService("github");

        var updated = vm.Services.First(s => s.ServiceType == "github");
        Assert.False(updated.Enabled);
    }

    [Fact]
    public void ToggleService_WhenServiceNotFound_ShouldNotThrow()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new UserServicesViewModel(kernelClient.Object);

        var ex = Record.Exception(() => vm.ToggleService("nonexistent"));

        Assert.Null(ex);
    }

    [Fact]
    public void LoadCommand_ShouldExist()
    {
        var vm = new UserServicesViewModel();
        Assert.NotNull(vm.LoadCommand);
    }

    [Fact]
    public void ToggleCommand_ShouldExist()
    {
        var vm = new UserServicesViewModel();
        Assert.NotNull(vm.ToggleCommand);
    }
}
