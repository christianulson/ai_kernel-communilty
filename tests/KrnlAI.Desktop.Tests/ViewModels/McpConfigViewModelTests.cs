using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class McpConfigViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new McpConfigViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Servers);
        Assert.Null(vm.SelectedServer);
    }

    [Fact]
    public async Task LoadServersAsync_ShouldCallGetMcpServersAsyncAndUpdate()
    {
        var kernelClient = new Mock<IKernelClient>();
        var servers = new List<McpServerInfo>
        {
            new("s1", "Server 1", "stdio", true, true, 3, DateTime.UtcNow),
            new("s2", "Server 2", "sse", false, false, 0, null),
        };
        kernelClient.Setup(k => k.GetMcpServersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(servers);

        var vm = new McpConfigViewModel(kernelClient.Object);

        await vm.LoadServersAsync();

        Assert.Equal(2, vm.Servers.Count);
        Assert.Equal("Server 1", vm.Servers[0].Name);
        Assert.True(vm.Servers[0].Enabled);
        Assert.False(vm.Servers[1].Enabled);
    }

    [Fact]
    public async Task LoadServersAsync_ShouldSetIsLoadingTrueDuringExecution()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<List<McpServerInfo>>();
        kernelClient.Setup(k => k.GetMcpServersAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new McpConfigViewModel(kernelClient.Object);

        var task = vm.LoadServersAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult([]);
        await task;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadServersAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetMcpServersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("servers error"));

        var vm = new McpConfigViewModel(kernelClient.Object);

        await vm.LoadServersAsync();

        Assert.True(vm.HasError);
        Assert.Contains("servers error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task ToggleServerAsync_ShouldCallToggleMcpServerAsync()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.ToggleMcpServerAsync("s1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vm = new McpConfigViewModel(kernelClient.Object);

        var result = await vm.ToggleServerAsync("s1", true);

        Assert.True(result);
    }

    [Fact]
    public async Task ToggleServerAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.ToggleMcpServerAsync("s1", true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("toggle error"));

        var vm = new McpConfigViewModel(kernelClient.Object);

        var result = await vm.ToggleServerAsync("s1", true);

        Assert.False(result);
        Assert.True(vm.HasError);
        Assert.Contains("toggle error", vm.ErrorMessage);
    }

    [Fact]
    public async Task UpdateServerAsync_ShouldCallUpdateMcpServerAsync()
    {
        var kernelClient = new Mock<IKernelClient>();
        var config = new McpServerConfig("s1", "Updated", "stdio", "cmd", null, null);
        kernelClient.Setup(k => k.UpdateMcpServerAsync("s1", config, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vm = new McpConfigViewModel(kernelClient.Object);

        var result = await vm.UpdateServerAsync("s1", config);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateServerAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        var config = new McpServerConfig("s1", "Updated", "stdio", "cmd", null, null);
        kernelClient.Setup(k => k.UpdateMcpServerAsync("s1", config, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("update error"));

        var vm = new McpConfigViewModel(kernelClient.Object);

        var result = await vm.UpdateServerAsync("s1", config);

        Assert.False(result);
        Assert.True(vm.HasError);
        Assert.Contains("update error", vm.ErrorMessage);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new McpConfigViewModel();
        vm.ErrorMessage = "some error";
        Assert.True(vm.HasError);
        vm.ClearError();
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new McpConfigViewModel();
        vm.ErrorMessage = "some error";
        vm.ClearErrorCommand.Execute(null);
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void LoadServersCommand_ShouldExist()
    {
        var vm = new McpConfigViewModel();
        Assert.NotNull(vm.LoadServersCommand);
    }

    [Fact]
    public void ToggleServerCommand_ShouldExist()
    {
        var vm = new McpConfigViewModel();
        Assert.NotNull(vm.ToggleServerCommand);
    }
}
