using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class CodingViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new CodingViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Code);
        Assert.Empty(vm.Language);
        Assert.Empty(vm.Description);
        Assert.Empty(vm.TestFramework);
        Assert.Null(vm.Result);
        Assert.Null(vm.Status);
    }

    [Fact]
    public async Task ExplainAsync_WhenCodeEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new CodingViewModel(kernelClient.Object);
        await vm.ExplainAsync();
        kernelClient.Verify(k => k.CodingExplainAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExplainAsync_WhenCodeWhitespace_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "   ";
        await vm.ExplainAsync();
        kernelClient.Verify(k => k.CodingExplainAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExplainAsync_ShouldCallCodingExplainAsyncAndUpdateResult()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingExplainAsync(It.Is<CodingRequest>(r => r.Code == "code"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodingResponse("result", "explanation", true, null));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";
        vm.Language = "csharp";
        vm.Description = "explain this";

        await vm.ExplainAsync();

        Assert.NotNull(vm.Result);
        Assert.Equal("result", vm.Result.Result);
        Assert.True(vm.Result.Success);
    }

    [Fact]
    public async Task ExplainAsync_ShouldSetIsLoadingTrueDuringExecution()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<CodingResponse?>();
        kernelClient.Setup(k => k.CodingExplainAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        var task = vm.ExplainAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult(new CodingResponse(null, null, false, null));
        await task;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task ExplainAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingExplainAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("explain failed"));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        await vm.ExplainAsync();

        Assert.True(vm.HasError);
        Assert.Contains("explain failed", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task FixAsync_ShouldCallCodingFixAsyncAndUpdateResult()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingFixAsync(It.Is<CodingRequest>(r => r.Code == "buggy"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodingResponse("fixed code", "fix applied", true, null));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "buggy";

        await vm.FixAsync();

        Assert.NotNull(vm.Result);
        Assert.Equal("fixed code", vm.Result.Result);
    }

    [Fact]
    public async Task FixAsync_WhenCodeEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new CodingViewModel(kernelClient.Object);
        await vm.FixAsync();
        kernelClient.Verify(k => k.CodingFixAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FixAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingFixAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("fix failed"));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        await vm.FixAsync();

        Assert.True(vm.HasError);
        Assert.Contains("fix failed", vm.ErrorMessage);
    }

    [Fact]
    public async Task GenerateTestsAsync_ShouldCallCodingGenerateTestsAsyncAndUpdateResult()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingGenerateTestsAsync(It.Is<CodingRequest>(r => r.Code == "class Foo"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodingResponse("[Fact]...", "tests generated", true, null));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "class Foo";
        vm.TestFramework = "xunit";

        await vm.GenerateTestsAsync();

        Assert.NotNull(vm.Result);
        Assert.Equal("[Fact]...", vm.Result.Result);
    }

    [Fact]
    public async Task GenerateTestsAsync_WhenCodeEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new CodingViewModel(kernelClient.Object);
        await vm.GenerateTestsAsync();
        kernelClient.Verify(k => k.CodingGenerateTestsAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateTestsAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingGenerateTestsAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("gen failed"));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        await vm.GenerateTestsAsync();

        Assert.True(vm.HasError);
        Assert.Contains("gen failed", vm.ErrorMessage);
    }

    [Fact]
    public async Task ReviewAsync_ShouldCallCodingReviewAsyncAndUpdateResult()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingReviewAsync(It.Is<CodingRequest>(r => r.Code == "code"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodingResponse("review comments", null, true, null));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        await vm.ReviewAsync();

        Assert.NotNull(vm.Result);
        Assert.Equal("review comments", vm.Result.Result);
    }

    [Fact]
    public async Task ReviewAsync_WhenCodeEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new CodingViewModel(kernelClient.Object);
        await vm.ReviewAsync();
        kernelClient.Verify(k => k.CodingReviewAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviewAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingReviewAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("review error"));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        await vm.ReviewAsync();

        Assert.True(vm.HasError);
        Assert.Contains("review error", vm.ErrorMessage);
    }

    [Fact]
    public async Task ApplyDiffAsync_ShouldCallCodingApplyDiffAsyncAndUpdateResult()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingApplyDiffAsync(It.Is<CodingRequest>(r => r.Code == "diff"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodingResponse("applied", "patch succeeded", true, null));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "diff";

        await vm.ApplyDiffAsync();

        Assert.NotNull(vm.Result);
        Assert.Equal("applied", vm.Result.Result);
    }

    [Fact]
    public async Task ApplyDiffAsync_WhenCodeEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new CodingViewModel(kernelClient.Object);
        await vm.ApplyDiffAsync();
        kernelClient.Verify(k => k.CodingApplyDiffAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyDiffAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingApplyDiffAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("diff error"));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        await vm.ApplyDiffAsync();

        Assert.True(vm.HasError);
        Assert.Contains("diff error", vm.ErrorMessage);
    }

    [Fact]
    public async Task CompleteAsync_ShouldCallCodingCompleteAsyncAndUpdateResult()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingCompleteAsync(It.Is<CodingRequest>(r => r.Code == "code"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodingResponse("completed", null, true, null));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        await vm.CompleteAsync();

        Assert.NotNull(vm.Result);
        Assert.Equal("completed", vm.Result.Result);
    }

    [Fact]
    public async Task CompleteAsync_WhenCodeEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new CodingViewModel(kernelClient.Object);
        await vm.CompleteAsync();
        kernelClient.Verify(k => k.CodingCompleteAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.CodingCompleteAsync(It.IsAny<CodingRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("complete error"));

        var vm = new CodingViewModel(kernelClient.Object);
        vm.Code = "code";

        await vm.CompleteAsync();

        Assert.True(vm.HasError);
        Assert.Contains("complete error", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadStatusAsync_ShouldCallGetCodingStatusAsyncAndUpdate()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetCodingStatusAsync("cycle-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodingStatus("cycle-1", "running", "testing", 0.5, null, null, DateTime.UtcNow, null));

        var vm = new CodingViewModel(kernelClient.Object);

        await vm.LoadStatusAsync("cycle-1");

        Assert.NotNull(vm.Status);
        Assert.Equal("running", vm.Status.Status);
        Assert.Equal(0.5, vm.Status.Progress);
    }

    [Fact]
    public async Task LoadStatusAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.GetCodingStatusAsync("cycle-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("status error"));

        var vm = new CodingViewModel(kernelClient.Object);

        await vm.LoadStatusAsync("cycle-1");

        Assert.True(vm.HasError);
        Assert.Contains("status error", vm.ErrorMessage);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new CodingViewModel();
        vm.ErrorMessage = "some error";
        Assert.True(vm.HasError);

        vm.ClearError();

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new CodingViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearErrorCommand.Execute(null);

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ExplainCommand_ShouldExist()
    {
        var vm = new CodingViewModel();
        Assert.NotNull(vm.ExplainCommand);
    }

    [Fact]
    public void FixCommand_ShouldExist()
    {
        var vm = new CodingViewModel();
        Assert.NotNull(vm.FixCommand);
    }

    [Fact]
    public void GenerateTestsCommand_ShouldExist()
    {
        var vm = new CodingViewModel();
        Assert.NotNull(vm.GenerateTestsCommand);
    }

    [Fact]
    public void ReviewCommand_ShouldExist()
    {
        var vm = new CodingViewModel();
        Assert.NotNull(vm.ReviewCommand);
    }

    [Fact]
    public void ApplyDiffCommand_ShouldExist()
    {
        var vm = new CodingViewModel();
        Assert.NotNull(vm.ApplyDiffCommand);
    }

    [Fact]
    public void CompleteCommand_ShouldExist()
    {
        var vm = new CodingViewModel();
        Assert.NotNull(vm.CompleteCommand);
    }

    [Fact]
    public void LoadStatusCommand_ShouldExist()
    {
        var vm = new CodingViewModel();
        Assert.NotNull(vm.LoadStatusCommand);
    }
}
