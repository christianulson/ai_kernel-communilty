using KrnlAI.Desktop.Core.Services;
using Xunit;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class EmotionalNormalizerTests
{
    [Theory]
    [InlineData(0.5, 0.2, "😌 Tranquilo")]
    [InlineData(0.5, 0.6, "⚡ Animado")]
    [InlineData(-0.5, 0.2, "😮‍💨 Cansado")]
    [InlineData(-0.5, 0.6, "😰 Tenso")]
    [InlineData(0.0, 0.5, "🧐 Atento")]
    [InlineData(0.0, 0.2, "😐 Neutro")]
    public void NormalizeMood_ShouldReturnCorrectEmotion(double valence, double arousal, string expected)
    {
        var result = EmotionalNormalizer.NormalizeMood(valence, arousal);
        Assert.Equal(expected, result);
    }
}
