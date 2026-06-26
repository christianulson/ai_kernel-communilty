using AutoFixture;
using Cts = KrnlAI.Contracts;
using KrnlAI.Desktop.Core.Services;
using Moq;
using TestHelpers;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ChatViewModelDiTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();
    private ChatViewModel CreateVm(Mock<IKernelClient>? kernelClient = null)
    {
        var kc = kernelClient?.Object ?? Mock.Of<IKernelClient>();
        var audio = Mock.Of<IAudioCapture>();
        var playback = Mock.Of<IAudioPlayback>();
        var video = Mock.Of<IVideoCapture>();
        var loc = Mock.Of<ILocalizationService>();
        var slash = Mock.Of<ISlashCommandExecutor>();
        var stream = Mock.Of<ICognitiveStreamProvider>();
        return new ChatViewModel(kc, audio, playback, video, loc, slash, stream);
    }

    [Fact]
    public async Task SendMessage_WithValidText_ShouldCallRunAgent()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.RunAgentAsync(It.IsAny<Cts.AgentRunTransportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cts.AgentRunTransportResponse("Hello!", null, null, null, null));
        kernelClient.Setup(k => k.GenerateSpeechAsync(It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var vm = CreateVm(kernelClient);
        vm.InputText = "test message";
        await vm.SendMessageAsync();

        kernelClient.Verify(k => k.RunAgentAsync(It.Is<Cts.AgentRunTransportRequest>(r => r.Prompt == "test message"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_ShouldAddUserAndAssistantMessages()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.RunAgentAsync(It.IsAny<Cts.AgentRunTransportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cts.AgentRunTransportResponse("AI response", null, null, null, null));
        kernelClient.Setup(k => k.GenerateSpeechAsync(It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var vm = CreateVm(kernelClient);
        vm.InputText = "hello";
        await vm.SendMessageAsync();

        Assert.Equal(2, vm.Messages.Count);
        Assert.Equal(MessageRole.User, vm.Messages[0].Role);
        Assert.Equal(MessageRole.Assistant, vm.Messages[1].Role);
        Assert.Equal("AI response", vm.Messages[1].Content);
    }

    [Fact]
    public async Task SendMessage_WhenApiFails_ShouldAddErrorMessage()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.RunAgentAsync(It.IsAny<Cts.AgentRunTransportRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var vm = CreateVm(kernelClient);
        vm.InputText = "test";
        await vm.SendMessageAsync();

        Assert.Equal(2, vm.Messages.Count);
        Assert.Equal(MessageRole.System, vm.Messages[1].Role);
        Assert.Contains("API unavailable", vm.Messages[1].Content);
    }

    [Fact]
    public async Task SendMessage_WithEmptyText_ShouldNotCallApi()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = CreateVm(kernelClient);
        await vm.SendMessageAsync();
        kernelClient.Verify(k => k.RunAgentAsync(It.IsAny<Cts.AgentRunTransportRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendMessage_ShouldClearInputAfterSending()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.RunAgentAsync(It.IsAny<Cts.AgentRunTransportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cts.AgentRunTransportResponse("done", null, null, null, null));
        kernelClient.Setup(k => k.GenerateSpeechAsync(It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var vm = CreateVm(kernelClient);
        vm.InputText = "test message";
        await vm.SendMessageAsync();

        Assert.Empty(vm.InputText);
        Assert.Equal(2, vm.Messages.Count);
        Assert.Equal("test message", vm.Messages[0].Content);
        Assert.Equal("done", vm.Messages[1].Content);
    }
}
