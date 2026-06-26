using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class ExperimentsViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new ExperimentsViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Experiments);
    }

    [Fact]
    public void Constructor_WithKernelClient_ShouldInitializeProperties()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new ExperimentsViewModel(kernelClient.Object);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Experiments);
    }

    [Fact]
    public async Task LoadExperimentsAsync_ShouldPopulateExperiments()
    {
        var kernelClient = new Mock<IKernelClient>();
        var experiments = new List<ExperimentInfo>
        {
            new("e1", "Exp 1", "running", "Testing accuracy", DateTime.UtcNow, null),
            new("e2", "Exp 2", "completed", "Testing speed", DateTime.UtcNow, DateTime.UtcNow),
        };
        kernelClient.Setup(k => k.ExperimentListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(experiments);

        var vm = new ExperimentsViewModel(kernelClient.Object);
        await vm.LoadExperimentsAsync();

        Assert.Equal(2, vm.Experiments.Count);
        Assert.Equal("Exp 1", vm.Experiments[0].Name);
        Assert.Equal("Exp 2", vm.Experiments[1].Name);
    }

    [Fact]
    public async Task LoadExperimentsAsync_ShouldSetIsLoading()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<List<ExperimentInfo>>();
        kernelClient.Setup(k => k.ExperimentListAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new ExperimentsViewModel(kernelClient.Object);
        var loadTask = vm.LoadExperimentsAsync();

        Assert.True(vm.IsLoading);
        tcs.SetResult([]);
        await loadTask;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadExperimentsAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.ExperimentListAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        var vm = new ExperimentsViewModel(kernelClient.Object);
        await vm.LoadExperimentsAsync();

        Assert.True(vm.HasError);
        Assert.Contains("API error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task StartExperimentAsync_ShouldAddToList()
    {
        var kernelClient = new Mock<IKernelClient>();
        var started = new ExperimentInfo("e-new", "New Exp", "running", "Testing", DateTime.UtcNow, null);
        kernelClient.Setup(k => k.ExperimentStartAsync(It.IsAny<StartExperimentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(started);
        kernelClient.Setup(k => k.ExperimentListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([started]);

        var vm = new ExperimentsViewModel(kernelClient.Object);
        vm.NewExperimentName = "New Exp";
        vm.NewExperimentDescription = "Testing";

        await vm.StartExperimentAsync();

        Assert.Single(vm.Experiments);
        Assert.Equal("New Exp", vm.Experiments[0].Name);
    }

    [Fact]
    public async Task StartExperimentAsync_WhenNameEmpty_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();

        var vm = new ExperimentsViewModel(kernelClient.Object);
        vm.NewExperimentName = "";

        await vm.StartExperimentAsync();

        kernelClient.Verify(k => k.ExperimentStartAsync(It.IsAny<StartExperimentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartExperimentAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.ExperimentStartAsync(It.IsAny<StartExperimentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("start error"));

        var vm = new ExperimentsViewModel(kernelClient.Object);
        vm.NewExperimentName = "Test";

        await vm.StartExperimentAsync();

        Assert.True(vm.HasError);
        Assert.Contains("start error", vm.ErrorMessage);
    }

    [Fact]
    public async Task StartExperimentAsync_ShouldClearFormOnSuccess()
    {
        var kernelClient = new Mock<IKernelClient>();
        var started = new ExperimentInfo("e-new", "Test", "running", "desc", DateTime.UtcNow, null);
        kernelClient.Setup(k => k.ExperimentStartAsync(It.IsAny<StartExperimentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(started);
        kernelClient.Setup(k => k.ExperimentListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var vm = new ExperimentsViewModel(kernelClient.Object);
        vm.NewExperimentName = "Test";
        vm.NewExperimentDescription = "desc";

        await vm.StartExperimentAsync();

        Assert.Empty(vm.NewExperimentName);
        Assert.Empty(vm.NewExperimentDescription);
    }

    [Fact]
    public async Task CompleteExperimentAsync_ShouldUpdateStatus()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.ExperimentCompleteAsync("e1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        kernelClient.Setup(k => k.ExperimentListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var vm = new ExperimentsViewModel(kernelClient.Object);
        vm.Experiments.Add(new ExperimentInfo("e1", "Exp 1", "running", "", DateTime.UtcNow, null));

        await vm.CompleteExperimentAsync("e1");

        kernelClient.Verify(k => k.ExperimentCompleteAsync("e1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordMetricAsync_ShouldCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.ExperimentRecordMetricAsync("e1", It.IsAny<RecordMetricRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var vm = new ExperimentsViewModel(kernelClient.Object);

        await vm.RecordMetricAsync("e1", "accuracy", 0.95);

        kernelClient.Verify(k => k.ExperimentRecordMetricAsync("e1",
            It.Is<RecordMetricRequest>(r => r.MetricName == "accuracy" && r.Value == 0.95),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ViewAnalysisAsync_ShouldSetAnalysis()
    {
        var kernelClient = new Mock<IKernelClient>();
        var analysis = new ExperimentAnalysis("e1", 5, 0.85, 120.5, 0.92,
            [new MetricEntry("accuracy", 0.95, DateTime.UtcNow)],
            ["Good performance"]);
        kernelClient.Setup(k => k.ExperimentGetAnalysisAsync("e1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        var vm = new ExperimentsViewModel(kernelClient.Object);

        await vm.ViewAnalysisAsync("e1");

        Assert.NotNull(vm.CurrentAnalysis);
        Assert.Equal(5, vm.CurrentAnalysis!.TotalMetrics);
        Assert.Equal(0.85, vm.CurrentAnalysis.AvgValue);
    }

    [Fact]
    public async Task ViewAnalysisAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.ExperimentGetAnalysisAsync("e1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("analysis error"));

        var vm = new ExperimentsViewModel(kernelClient.Object);

        await vm.ViewAnalysisAsync("e1");

        Assert.True(vm.HasError);
        Assert.Null(vm.CurrentAnalysis);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new ExperimentsViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearError();

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearAnalysis_ShouldClearCurrentAnalysis()
    {
        var vm = new ExperimentsViewModel();
        vm.CurrentAnalysis = new ExperimentAnalysis("e1", 0, 0, 0, 0, [], []);

        vm.ClearAnalysis();

        Assert.Null(vm.CurrentAnalysis);
    }

    [Fact]
    public void LoadExperimentsCommand_ShouldExist()
    {
        var vm = new ExperimentsViewModel();
        Assert.NotNull(vm.LoadExperimentsCommand);
    }

    [Fact]
    public void StartExperimentCommand_ShouldExist()
    {
        var vm = new ExperimentsViewModel();
        Assert.NotNull(vm.StartExperimentCommand);
    }

    [Fact]
    public void CompleteExperimentCommand_ShouldExist()
    {
        var vm = new ExperimentsViewModel();
        Assert.NotNull(vm.CompleteExperimentCommand);
    }

    [Fact]
    public void RecordMetricCommand_ShouldExist()
    {
        var vm = new ExperimentsViewModel();
        Assert.NotNull(vm.RecordMetricCommand);
    }

    [Fact]
    public void ViewAnalysisCommand_ShouldExist()
    {
        var vm = new ExperimentsViewModel();
        Assert.NotNull(vm.ViewAnalysisCommand);
    }
}
