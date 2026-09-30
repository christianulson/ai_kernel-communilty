using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Infrastructure.Speech;
using KrnlAI.Embedded.Abstractions;
using Moq;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class SpeechSynthesisTests
{
    [Fact]
    public async Task WindowsService_EmptyText_ShouldReturnEmpty()
    {
        var service = new WindowsSpeechSynthesisService();

        var audio = await service.SynthesizeAsync("   ");

        Assert.Empty(audio);
    }

    [Fact]
    public async Task WindowsService_ShortText_ShouldReturnWaveOrEmptyGracefully()
    {
        var service = new WindowsSpeechSynthesisService();

        var audio = await service.SynthesizeAsync("ola");

        if (audio.Length > 0)
        {
            Assert.True(audio.Length > 44, "WAV deve ter header + dados");
            Assert.Equal((byte)'R', audio[0]);
            Assert.Equal((byte)'I', audio[1]);
            Assert.Equal((byte)'F', audio[2]);
            Assert.Equal((byte)'F', audio[3]);
        }
    }

    [Fact]
    public async Task EmbeddedClient_WithSpeechService_ShouldReturnSynthesizedAudio()
    {
        var speech = new Mock<ISpeechSynthesisService>();
        speech
            .Setup(s => s.SynthesizeAsync("oi", It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });
        var client = new EmbeddedKernelClient(Mock.Of<IEmbeddedKrnlAI>(), speech.Object);

        var audio = await client.GenerateSpeechAsync("oi");

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, audio);
    }

    [Fact]
    public async Task EmbeddedClient_WithoutSpeechService_ShouldFallbackToStubHeader()
    {
        var client = new EmbeddedKernelClient(Mock.Of<IEmbeddedKrnlAI>());

        var audio = await client.GenerateSpeechAsync("oi");

        Assert.True(audio.Length > 0);
        Assert.Equal((byte)'R', audio[0]);
    }

    [Fact]
    public async Task EmbeddedClient_SpeechServiceFails_ShouldFallbackToStubHeader()
    {
        var speech = new Mock<ISpeechSynthesisService>();
        speech
            .Setup(s => s.SynthesizeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("no engine"));
        var client = new EmbeddedKernelClient(Mock.Of<IEmbeddedKrnlAI>(), speech.Object);

        var audio = await client.GenerateSpeechAsync("oi");

        Assert.True(audio.Length > 0);
        Assert.Equal((byte)'R', audio[0]);
    }

    [Fact]
    public async Task EmbeddedClient_EmptySynthesis_ShouldFallbackToStubHeader()
    {
        var speech = new Mock<ISpeechSynthesisService>();
        speech
            .Setup(s => s.SynthesizeAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<byte>());
        var client = new EmbeddedKernelClient(Mock.Of<IEmbeddedKrnlAI>(), speech.Object);

        var audio = await client.GenerateSpeechAsync("oi");

        Assert.True(audio.Length > 0);
        Assert.Equal((byte)'R', audio[0]);
    }
}
