using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class AssistantViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new AssistantViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Content);
        Assert.Empty(vm.Threads);
        Assert.Empty(vm.Messages);
        Assert.Null(vm.ActiveThread);
        Assert.Null(vm.ActiveRun);
    }

    [Fact]
    public async Task CreateThreadAsync_ShouldCallCreateThreadAsyncAndAddThread()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CreateThreadAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThreadInfo("t1", "test", DateTime.UtcNow, "active"));

        var vm = new AssistantViewModel(kernelClient.Object);

        await vm.CreateThreadAsync("test");

        Assert.Single(vm.Threads);
        Assert.Equal("t1", vm.Threads[0].ThreadId);
    }

    [Fact]
    public async Task CreateThreadAsync_WhenResultIsNull_ShouldNotAddThread()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CreateThreadAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThreadInfo?)null);

        var vm = new AssistantViewModel(kernelClient.Object);

        await vm.CreateThreadAsync("test");

        Assert.Empty(vm.Threads);
    }

    [Fact]
    public async Task CreateThreadAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CreateThreadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("create error"));

        var vm = new AssistantViewModel(kernelClient.Object);

        await vm.CreateThreadAsync("test");

        Assert.True(vm.HasError);
        Assert.Contains("create error", vm.ErrorMessage);
    }

    [Fact]
    public async Task SelectThreadAsync_ShouldCallGetMessagesAndUpdate()
    {
        var kernelClient = new Mock<IKernelClient>();
        var messages = new List<MessageInfo>
        {
            new("m1", "t1", "user", "hello", DateTime.UtcNow, null),
            new("m2", "t1", "assistant", "hi", DateTime.UtcNow, null),
        };
        kernelClient.Setup(k => k.GetMessagesAsync("t1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);
        kernelClient.Setup(k => k.GetThreadAsync("t1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThreadInfo("t1", "test", DateTime.UtcNow, "active"));

        var vm = new AssistantViewModel(kernelClient.Object);
        vm.Threads.Add(new ThreadInfo("t1", "test", DateTime.UtcNow, "active"));

        await vm.SelectThreadAsync("t1");

        Assert.NotNull(vm.ActiveThread);
        Assert.Equal("t1", vm.ActiveThread.ThreadId);
        Assert.Equal(2, vm.Messages.Count);
    }

    [Fact]
    public async Task SendMessageAsync_WhenContentEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new AssistantViewModel(kernelClient.Object);
        await vm.SendMessageAsync("t1", "");
        kernelClient.Verify(k => k.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldCallSendMessageAndAddMessage()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.SendMessageAsync("t1", "hello", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MessageInfo("m1", "t1", "user", "hello", DateTime.UtcNow, null));

        var vm = new AssistantViewModel(kernelClient.Object);
        vm.ActiveThread = new ThreadInfo("t1", "test", DateTime.UtcNow, "active");

        await vm.SendMessageAsync("t1", "hello");

        Assert.Single(vm.Messages);
        Assert.Equal("hello", vm.Messages[0].Content);
    }

    [Fact]
    public async Task SendMessageAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.SendMessageAsync("t1", "hello", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("send error"));

        var vm = new AssistantViewModel(kernelClient.Object);
        vm.ActiveThread = new ThreadInfo("t1", "test", DateTime.UtcNow, "active");

        await vm.SendMessageAsync("t1", "hello");

        Assert.True(vm.HasError);
        Assert.Contains("send error", vm.ErrorMessage);
    }

    [Fact]
    public async Task CreateRunAsync_ShouldCallCreateRunAsyncAndUpdateActiveRun()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CreateRunAsync("t1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunInfo("r1", "t1", "running", "thinking", DateTime.UtcNow, null, null));

        var vm = new AssistantViewModel(kernelClient.Object);

        await vm.CreateRunAsync("t1");

        Assert.NotNull(vm.ActiveRun);
        Assert.Equal("r1", vm.ActiveRun.RunId);
        Assert.Equal("running", vm.ActiveRun.Status);
    }

    [Fact]
    public async Task CreateRunAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CreateRunAsync("t1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("run error"));

        var vm = new AssistantViewModel(kernelClient.Object);

        await vm.CreateRunAsync("t1");

        Assert.True(vm.HasError);
        Assert.Contains("run error", vm.ErrorMessage);
    }

    [Fact]
    public async Task GetRunAsync_ShouldCallGetRunAsyncAndUpdateActiveRun()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetRunAsync("t1", "r1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunInfo("r1", "t1", "completed", null, DateTime.UtcNow, DateTime.UtcNow, null));

        var vm = new AssistantViewModel(kernelClient.Object);

        await vm.GetRunAsync("t1", "r1");

        Assert.NotNull(vm.ActiveRun);
        Assert.Equal("completed", vm.ActiveRun.Status);
    }

    [Fact]
    public async Task LoadThreadsAsync_ShouldPopulateThreads()
    {
        var kernelClient = new Mock<IKernelClient>();

        var vm = new AssistantViewModel(kernelClient.Object);

        await vm.LoadThreadsAsync();

        Assert.Empty(vm.Threads);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new AssistantViewModel();
        vm.ErrorMessage = "some error";
        Assert.True(vm.HasError);
        vm.ClearError();
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new AssistantViewModel();
        vm.ErrorMessage = "some error";
        vm.ClearErrorCommand.Execute(null);
        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void CreateThreadCommand_ShouldExist()
    {
        var vm = new AssistantViewModel();
        Assert.NotNull(vm.CreateThreadCommand);
    }

    [Fact]
    public void SendMessageCommand_ShouldExist()
    {
        var vm = new AssistantViewModel();
        Assert.NotNull(vm.SendMessageCommand);
    }

    [Fact]
    public void CreateRunCommand_ShouldExist()
    {
        var vm = new AssistantViewModel();
        Assert.NotNull(vm.CreateRunCommand);
    }

    [Fact]
    public void LoadThreadsCommand_ShouldExist()
    {
        var vm = new AssistantViewModel();
        Assert.NotNull(vm.LoadThreadsCommand);
    }
}
