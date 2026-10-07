using KrnlAI.Contracts;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Embedded.Abstractions;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class MindViewModelTests
{
    [Fact]
    public async Task LoadAsync_WithSnapshot_ShouldPopulateState()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel
            .Setup(k => k.GetMindSnapshotAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MindSnapshot(
                ["25/09 15:00 reuniao"],
                ["comprar cafe"],
                ["explore_memory"],
                "positive",
                0.8,
                0.3,
                2,
                DateTimeOffset.UtcNow));
        var viewModel = new MindViewModel(kernel.Object);

        await viewModel.LoadAsync();

        Assert.Single(viewModel.Agenda);
        Assert.Contains("reuniao", viewModel.Agenda[0]);
        Assert.Single(viewModel.Notes);
        Assert.Single(viewModel.Goals);
        Assert.Equal("positive", viewModel.EmotionalTone);
        Assert.Equal(0.8, viewModel.Valence);
        Assert.Equal(2, viewModel.LearnedProcedures);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_WithLearningFocus_ShouldPopulateFocus()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel
            .Setup(k => k.GetMindSnapshotAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MindSnapshot(
                [],
                [],
                [],
                "neutral",
                0,
                0,
                0,
                DateTimeOffset.UtcNow,
                LearningFocus: "Improve safety (Safety)"));
        var viewModel = new MindViewModel(kernel.Object);

        await viewModel.LoadAsync();

        Assert.Equal("Improve safety (Safety)", viewModel.LearningFocus);
    }

    [Fact]
    public async Task LoadAsync_WithoutKernel_ShouldNotThrow()
    {
        var viewModel = new MindViewModel();

        var exception = await Record.ExceptionAsync(viewModel.LoadAsync);

        Assert.Null(exception);
    }

    [Fact]
    public async Task LoadAsync_KernelThrows_ShouldFailGracefully()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel
            .Setup(k => k.GetMindSnapshotAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("kernel offline"));
        var viewModel = new MindViewModel(kernel.Object);

        var exception = await Record.ExceptionAsync(viewModel.LoadAsync);

        Assert.Null(exception);
        Assert.False(viewModel.IsLoading);
    }
}
