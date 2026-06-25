using Moq;
using System.Net.Http;

namespace KrnlAI.Desktop.Tests.Controls;

public class PieViewModelTests
{
    private readonly Mock<IKernelClient> _kernelMock;
    private readonly PieViewModel _vm;

    public PieViewModelTests()
    {
        _kernelMock = new Mock<IKernelClient>();
        _vm = new PieViewModel(_kernelMock.Object);
    }

    [Fact]
    public async Task InferAsync_ValidPremise_ShouldSetConclusion()
    {
        var response = new PieInferResponse("Therefore X", 0.92, ["evidence1"]);
        _kernelMock.Setup(k => k.PieInferAsync("All men are mortal", "Socrates", default))
            .ReturnsAsync(response);

        _vm.Premise = "All men are mortal";
        _vm.Context = "Socrates";
        await _vm.InferAsync();

        Assert.Equal("Therefore X", _vm.Conclusion);
        Assert.Equal(0.92, _vm.Confidence);
    }

    [Fact]
    public async Task InferAsync_EmptyPremise_ShouldNotCallApi()
    {
        _vm.Premise = "";
        await _vm.InferAsync();

        _kernelMock.Verify(k => k.PieInferAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ChainAsync_ShouldPopulateSteps()
    {
        var steps = new List<PieChainStep>
        {
            new(1, "A", "B", 0.9),
            new(2, "B", "C", 0.8),
        };
        _kernelMock.Setup(k => k.PieChainAsync("A", 2, "ctx", default))
            .ReturnsAsync(new PieChainResponse(steps));

        _vm.ChainPremise = "A";
        _vm.ChainSteps = 2;
        _vm.ChainContext = "ctx";
        await _vm.ChainAsync();

        Assert.Equal(2, _vm.ChainResults.Count);
        Assert.Equal("A", _vm.ChainResults[0].Premise);
    }

    [Fact]
    public async Task LoadTermsAsync_ShouldPopulateTerms()
    {
        var terms = new List<PieTerm>
        {
            new("t1", "Logic", "Logical reasoning", 10),
            new("t2", "Causality", null, 5),
        };
        _kernelMock.Setup(k => k.PieTermsAsync(default))
            .ReturnsAsync(terms);

        await _vm.LoadTermsAsync();

        Assert.Equal(2, _vm.Terms.Count);
        Assert.Equal("Logic", _vm.Terms[0].Name);
    }

    [Fact]
    public async Task LoadCoherenceAsync_ShouldSetCoherence()
    {
        var entries = new List<PieCoherenceEntry>
        {
            new("e1", "Statement 1", 0.85),
            new("e2", "Statement 2", 0.65),
        };
        _kernelMock.Setup(k => k.PieCoherenceAsync(default))
            .ReturnsAsync(new PieCoherenceData(0.75, entries));

        await _vm.LoadCoherenceAsync();

        Assert.NotNull(_vm.Coherence);
        Assert.Equal(0.75, _vm.Coherence.OverallCoherence);
    }

    [Fact]
    public async Task LearnFactAsync_ShouldCallApi()
    {
        _kernelMock.Setup(k => k.PieKnowledgeAsync("math", "2+2=4", 1.0, default))
            .ReturnsAsync(new PieKnowledgeResponse(true));

        var success = await _vm.LearnFactAsync("math", "2+2=4", 1.0);

        Assert.True(success);
    }

    [Fact]
    public async Task InferAsync_ApiError_ShouldSetError()
    {
        _kernelMock.Setup(k => k.PieInferAsync("test", "", default))
            .ThrowsAsync(new HttpRequestException("PIE error"));

        _vm.Premise = "test";
        await _vm.InferAsync();

        Assert.True(_vm.HasError);
        Assert.Contains("PIE error", _vm.ErrorMessage);
    }

    [Fact]
    public void ClearError_ShouldClearMessage()
    {
        _vm.ErrorMessage = "error";
        _vm.ClearError();
        Assert.False(_vm.HasError);
    }
}

