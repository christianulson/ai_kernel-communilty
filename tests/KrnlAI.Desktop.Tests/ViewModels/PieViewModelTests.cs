using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class PieViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new PieViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Premise);
        Assert.Empty(vm.Context);
        Assert.Empty(vm.Conclusion);
        Assert.Empty(vm.ChainPremise);
        Assert.Empty(vm.ChainContext);
        Assert.Empty(vm.ChainResults);
        Assert.Empty(vm.Terms);
        Assert.Equal(3, vm.ChainSteps);
        Assert.Equal(0, vm.Confidence);
        Assert.Null(vm.Coherence);
    }

    [Fact]
    public async Task InferAsync_WhenPremiseEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new PieViewModel(kernelClient.Object);

        await vm.InferAsync();

        kernelClient.Verify(k => k.PieInferAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InferAsync_ShouldCallPieInferAsyncAndUpdateResult()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.PieInferAsync("all men are mortal", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PieInferResponse("Socrates is mortal", 0.95, null));

        var vm = new PieViewModel(kernelClient.Object);
        vm.Premise = "all men are mortal";
        vm.Context = "Socrates";

        await vm.InferAsync();

        Assert.Equal("Socrates is mortal", vm.Conclusion);
        Assert.Equal(0.95, vm.Confidence);
    }

    [Fact]
    public async Task InferAsync_WhenResultIsNull_ShouldNotChangeConclusion()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.PieInferAsync("premise", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PieInferResponse?)null);

        var vm = new PieViewModel(kernelClient.Object);
        vm.Premise = "premise";

        await vm.InferAsync();

        Assert.Empty(vm.Conclusion);
        Assert.Equal(0, vm.Confidence);
    }

    [Fact]
    public async Task InferAsync_ShouldManageLoadingState()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<PieInferResponse?>();
        kernelClient.Setup(k => k.PieInferAsync("premise", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new PieViewModel(kernelClient.Object);
        vm.Premise = "premise";

        var task = vm.InferAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult(new PieInferResponse("conclusion", 0.8, null));
        await task;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task InferAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.PieInferAsync("premise", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("inference failed"));

        var vm = new PieViewModel(kernelClient.Object);
        vm.Premise = "premise";

        await vm.InferAsync();

        Assert.True(vm.HasError);
        Assert.Contains("inference failed", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task ChainAsync_WhenPremiseEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new PieViewModel(kernelClient.Object);

        await vm.ChainAsync();

        kernelClient.Verify(k => k.PieChainAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ChainAsync_ShouldCallPieChainAsyncAndUpdateResults()
    {
        var kernelClient = new Mock<IKernelClient>();
        var steps = new List<PieChainStep>
        {
            new(1, "A", "B", 0.9),
            new(2, "B", "C", 0.8),
        };
        kernelClient.Setup(k => k.PieChainAsync("start", 3, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PieChainResponse(steps));

        var vm = new PieViewModel(kernelClient.Object);
        vm.ChainPremise = "start";

        await vm.ChainAsync();

        Assert.Equal(2, vm.ChainResults.Count);
        Assert.Equal("B", vm.ChainResults[0].Conclusion);
        Assert.Equal("C", vm.ChainResults[1].Conclusion);
    }

    [Fact]
    public async Task ChainAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.PieChainAsync("start", 3, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("chain error"));

        var vm = new PieViewModel(kernelClient.Object);
        vm.ChainPremise = "start";

        await vm.ChainAsync();

        Assert.True(vm.HasError);
        Assert.Contains("chain error", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadTermsAsync_ShouldCallPieTermsAsyncAndUpdateTerms()
    {
        var kernelClient = new Mock<IKernelClient>();
        var terms = new List<PieTerm>
        {
            new("t1", "term1", "desc1", 10),
            new("t2", "term2", "desc2", 5),
        };
        kernelClient.Setup(k => k.PieTermsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(terms);

        var vm = new PieViewModel(kernelClient.Object);

        await vm.LoadTermsAsync();

        Assert.Equal(2, vm.Terms.Count);
        Assert.Equal("term1", vm.Terms[0].Name);
    }

    [Fact]
    public async Task LoadTermsAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.PieTermsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("terms error"));

        var vm = new PieViewModel(kernelClient.Object);

        await vm.LoadTermsAsync();

        Assert.True(vm.HasError);
        Assert.Contains("terms error", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadCoherenceAsync_ShouldCallPieCoherenceAsyncAndUpdateCoherence()
    {
        var kernelClient = new Mock<IKernelClient>();
        var coherence = new PieCoherenceData(0.85, [new("e1", "statement", 0.9)]);
        kernelClient.Setup(k => k.PieCoherenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(coherence);

        var vm = new PieViewModel(kernelClient.Object);

        await vm.LoadCoherenceAsync();

        Assert.NotNull(vm.Coherence);
        Assert.Equal(0.85, vm.Coherence.OverallCoherence);
    }

    [Fact]
    public async Task LoadCoherenceAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.PieCoherenceAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("coherence error"));

        var vm = new PieViewModel(kernelClient.Object);

        await vm.LoadCoherenceAsync();

        Assert.True(vm.HasError);
        Assert.Contains("coherence error", vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new PieViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearErrorCommand.Execute(null);

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void InferCommand_ShouldExist()
    {
        var vm = new PieViewModel();
        Assert.NotNull(vm.InferCommand);
    }

    [Fact]
    public void ChainCommand_ShouldExist()
    {
        var vm = new PieViewModel();
        Assert.NotNull(vm.ChainCommand);
    }

    [Fact]
    public void LoadTermsCommand_ShouldExist()
    {
        var vm = new PieViewModel();
        Assert.NotNull(vm.LoadTermsCommand);
    }

    [Fact]
    public void LoadCoherenceCommand_ShouldExist()
    {
        var vm = new PieViewModel();
        Assert.NotNull(vm.LoadCoherenceCommand);
    }
}
