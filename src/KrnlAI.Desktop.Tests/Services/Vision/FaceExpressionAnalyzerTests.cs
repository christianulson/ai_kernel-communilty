using KrnlAI.Desktop.Core.Services.Vision;

namespace KrnlAI.Desktop.Tests.Services.Vision;

public sealed class FaceExpressionAnalyzerTests
{
    [Fact]
    public void AnalyzeFrame_NullFrame_ShouldReturnNull()
    {
        var analyzer = new FaceExpressionAnalyzer();
        var result = analyzer.AnalyzeFrame([], 0, 0);
        Assert.Null(result);
    }

    [Fact]
    public void AnalyzeFrame_TinyFrame_ShouldReturnNull()
    {
        var analyzer = new FaceExpressionAnalyzer();
        var result = analyzer.AnalyzeFrame(new byte[100], 10, 10);
        Assert.Null(result);
    }

    [Fact]
    public void AnalyzeFrame_EmptyFrame_ShouldNotThrow()
    {
        var analyzer = new FaceExpressionAnalyzer();
        var exception = Record.Exception(() => analyzer.AnalyzeFrame(new byte[640 * 480 * 3], 640, 480));
        Assert.Null(exception);
    }
}
