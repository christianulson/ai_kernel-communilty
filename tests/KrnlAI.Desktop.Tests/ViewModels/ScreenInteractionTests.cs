using System.Windows.Input;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ScreenInteractionTests
{
    [Fact]
    public void Assistant_SelectThreadCommand_ShouldOpenClickedThread()
    {
        var client = new Mock<IKernelClient>();
        var thread = new ThreadInfo("thread-2", "Second", DateTimeOffset.UnixEpoch, "active");
        client.Setup(c => c.GetThreadAsync("thread-2", default)).ReturnsAsync(thread);
        client.Setup(c => c.GetMessagesAsync("thread-2", default)).ReturnsAsync([]);
        var vm = new AssistantViewModel(client.Object);
        Command(vm, "SelectThreadCommand").Execute("thread-2");
        Assert.Equal(thread, vm.ActiveThread);
    }

    [Fact]
    public void Events_SelectEventCommand_ShouldOpenClickedEvent()
    {
        var client = new Mock<IKernelClient>();
        var detail = new EventDetail("event-2", "type", "detail", null, DateTimeOffset.UnixEpoch, null, null, null);
        client.Setup(c => c.EventDetailAsync("event-2", default)).ReturnsAsync(detail);
        var vm = new EventsViewModel(client.Object);
        Command(vm, "SelectEventCommand").Execute("event-2");
        Assert.Equal(detail, vm.SelectedEvent);
    }

    [Fact]
    public void Episodes_SelectEpisodeCommand_ShouldLoadClickedEpisode()
    {
        var client = new Mock<IKernelClient>();
        var detail = new EpisodeDetails("episode-2", "goal", "done", DateTime.UnixEpoch, null, null, null, null, "Summary", []);
        client.Setup(c => c.GetEpisodeAsync("episode-2", default)).ReturnsAsync(detail);
        var vm = new EpisodesViewModel(client.Object);
        Command(vm, "SelectEpisodeCommand").Execute("episode-2");
        Assert.Equal(detail, vm.EpisodeDetail);
    }

    [Fact]
    public void Assistant_SendFailure_ShouldKeepDraft()
    {
        var client = new Mock<IKernelClient>();
        client.Setup(c => c.SendMessageAsync("thread", "my draft", default)).ThrowsAsync(new HttpRequestException("offline"));
        var vm = new AssistantViewModel(client.Object)
        {
            ActiveThread = new ThreadInfo("thread", "Title", DateTimeOffset.UnixEpoch, "active"),
            Content = "my draft"
        };
        vm.SendMessageCommand.Execute(vm.Content);
        Assert.True(vm.HasError);
        Assert.Equal("my draft", vm.Content);
    }

    private static ICommand Command(object vm, string property) =>
        Assert.IsAssignableFrom<ICommand>(vm.GetType().GetProperty(property)?.GetValue(vm));
}
