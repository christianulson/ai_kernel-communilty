using Moq;
using System.Net.Http;

namespace KrnlAI.Desktop.Tests.Controls;

public class EventsViewModelTests
{
    private readonly Mock<IKernelClient> _kernelMock;
    private readonly EventsViewModel _vm;

    public EventsViewModelTests()
    {
        _kernelMock = new Mock<IKernelClient>();
        _vm = new EventsViewModel(_kernelMock.Object);
    }

    [Fact]
    public async Task LoadRecentEventsAsync_ShouldPopulateEvents()
    {
        var events = new List<EventInfo>
        {
            new("e1", "cognitive", "Thought processed", "engine", DateTimeOffset.UtcNow, null),
            new("e2", "action", "File saved", "fs", DateTimeOffset.UtcNow.AddSeconds(-1), null),
        };
        _kernelMock.Setup(k => k.EventsRecentAsync(50, default))
            .ReturnsAsync(events);

        await _vm.LoadRecentEventsAsync().ConfigureAwait(false);

        Assert.Equal(2, _vm.Events.Count);
    }

    [Fact]
    public async Task LoadEventDetailAsync_ShouldSetDetail()
    {
        var detail = new EventDetail("e1", "cognitive", "Thought processed", "engine",
            DateTimeOffset.UtcNow, new() { ["key"] = "value" }, "entity-1", "goal");
        _kernelMock.Setup(k => k.EventDetailAsync("e1", default))
            .ReturnsAsync(detail);

        await _vm.LoadEventDetailAsync("e1").ConfigureAwait(false);

        Assert.NotNull(_vm.SelectedEvent);
        Assert.Equal("Thought processed", _vm.SelectedEvent.Description);
    }

    [Fact]
    public async Task LoadEventsByMomentAsync_ShouldPopulateEvents()
    {
        var events = new List<EventInfo>
        {
            new("e1", "cognitive", "Event in moment", "engine", DateTimeOffset.UtcNow, null),
        };
        _kernelMock.Setup(k => k.EventsByMomentAsync("moment-1", default))
            .ReturnsAsync(events);

        await _vm.LoadEventsByMomentAsync("moment-1").ConfigureAwait(false);

        Assert.Single(_vm.Events);
    }

    [Fact]
    public async Task LoadRecentEventsAsync_ApiError_ShouldSetError()
    {
        _kernelMock.Setup(k => k.EventsRecentAsync(50, default))
            .ThrowsAsync(new HttpRequestException("events unavailable"));

        await _vm.LoadRecentEventsAsync().ConfigureAwait(false);

        Assert.True(_vm.HasError);
    }

    [Fact]
    public async Task FilterByType_ShouldFilterList()
    {
        var events = new List<EventInfo>
        {
            new("e1", "cognitive", "Thought", "engine", DateTimeOffset.UtcNow, null),
            new("e2", "action", "Save", "fs", DateTimeOffset.UtcNow, null),
            new("e3", "cognitive", "Memory recall", "memory", DateTimeOffset.UtcNow, null),
        };
        _kernelMock.Setup(k => k.EventsRecentAsync(50, default))
            .ReturnsAsync(events);

        await _vm.LoadRecentEventsAsync().ConfigureAwait(false);
        _vm.SetTypeFilter("cognitive");

        Assert.Equal(2, _vm.FilteredEvents.Count);
    }

    [Fact]
    public void FilterByType_NoFilter_ShowsAll()
    {
        _vm.SetTypeFilter("");
        // Should just not filter, not crash
    }

    [Fact]
    public void ClearError_ShouldClear()
    {
        _vm.ErrorMessage = "err";
        _vm.ClearError();
        Assert.False(_vm.HasError);
    }
}

