using KrnlAI.Core.Abstractions.Autonomy;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Embedded.Abstractions;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ChatViewModelInitiativeTests
{
    private static ChatViewModel CreateVm(Mock<IEmbeddedKrnlAI> kernel) =>
        new(
            Mock.Of<IKernelClient>(),
            Mock.Of<IAudioCapture>(),
            Mock.Of<IAudioPlayback>(),
            Mock.Of<IVideoCapture>(),
            Mock.Of<ILocalizationService>(),
            Mock.Of<ISlashCommandExecutor>(),
            Mock.Of<ICognitiveStreamProvider>(),
            sessionStore: null,
            embeddedKernel: kernel.Object);

    [Fact]
    public async Task PollInitiatives_WithPending_ShouldAppendProactiveMessages()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(k => k.DrainInitiatives()).Returns([
            new InitiativeMessage("m1", "Tenho 2 metas autônomas.", 0.4, "goal_count", DateTimeOffset.UtcNow)
        ]);
        var vm = CreateVm(kernel);

        var count = await vm.PollInitiativesAsync();

        Assert.Equal(1, count);
        var message = Assert.Single(vm.Messages);
        Assert.True(message.IsProactive);
        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.Contains("Tenho 2 metas", message.Content);
    }

    [Fact]
    public async Task PollInitiatives_WithoutPending_ShouldReturnZero()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel.Setup(k => k.DrainInitiatives()).Returns([]);
        var vm = CreateVm(kernel);

        var count = await vm.PollInitiativesAsync();

        Assert.Equal(0, count);
        Assert.Empty(vm.Messages);
    }

    [Fact]
    public async Task SubmitFeedback_OnAssistantMessage_ShouldCallKernel()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        kernel
            .Setup(k => k.RecordFeedbackAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var vm = CreateVm(kernel);
        var message = new ChatMessage("id-1", "resposta", MessageRole.Assistant, DateTime.Now, MessageStatus.Completed);

        await vm.SubmitFeedbackAsync(message, positive: true);

        kernel.Verify(k => k.RecordFeedbackAsync("resposta", true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitFeedback_OnUserMessage_ShouldNotCallKernel()
    {
        var kernel = new Mock<IEmbeddedKrnlAI>();
        var vm = CreateVm(kernel);
        var message = new ChatMessage("id-2", "pergunta", MessageRole.User, DateTime.Now, MessageStatus.Completed);

        await vm.SubmitFeedbackAsync(message, positive: false);

        kernel.Verify(
            k => k.RecordFeedbackAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
