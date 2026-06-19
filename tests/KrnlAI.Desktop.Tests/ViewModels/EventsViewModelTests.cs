using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Moq;

namespace KrnlAI.Desktop.Tests.ViewModels;

[Trait("Category", "Unit")]
public sealed class EventsViewModelTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeProperties()
    {
        var vm = new EventsViewModel();
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Empty(vm.Events);
        Assert.Empty(vm.FilteredEvents);
        Assert.Null(vm.SelectedEvent);
        Assert.Empty(vm.MomentId);
    }

    [Fact]
    public async Task LoadRecentEventsAsync_ShouldCallEventsRecentAsyncAndUpdateEvents()
    {
        var kernelClient = new Mock<IKernelClient>();
        var events = new List<EventInfo>
        {
            new("e1", "type1", "desc1", "src1", DateTimeOffset.UtcNow, null),
            new("e2", "type2", "desc2", "src2", DateTimeOffset.UtcNow, null),
        };
        kernelClient.Setup(k => k.EventsRecentAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var vm = new EventsViewModel(kernelClient.Object);
        await vm.LoadRecentEventsAsync();

        Assert.Equal(2, vm.Events.Count);
        Assert.Equal(2, vm.FilteredEvents.Count);
        Assert.Equal("e1", vm.Events[0].EventId);
        Assert.Equal("desc1", vm.Events[0].Description);
    }

    [Fact]
    public async Task LoadRecentEventsAsync_ShouldManageLoadingState()
    {
        var kernelClient = new Mock<IKernelClient>();
        var tcs = new TaskCompletionSource<List<EventInfo>>();
        kernelClient.Setup(k => k.EventsRecentAsync(50, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var vm = new EventsViewModel(kernelClient.Object);
        var task = vm.LoadRecentEventsAsync();
        Assert.True(vm.IsLoading);
        tcs.SetResult([]);
        await task;
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadRecentEventsAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.EventsRecentAsync(50, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("events error"));

        var vm = new EventsViewModel(kernelClient.Object);
        await vm.LoadRecentEventsAsync();

        Assert.True(vm.HasError);
        Assert.Contains("events error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task LoadEventDetailAsync_ShouldCallEventDetailAsyncAndUpdateSelectedEvent()
    {
        var kernelClient = new Mock<IKernelClient>();
        var detail = new EventDetail("e1", "type1", "detail desc", "src1", DateTimeOffset.UtcNow, null, "rel1", "relType1");
        kernelClient.Setup(k => k.EventDetailAsync("e1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var vm = new EventsViewModel(kernelClient.Object);
        await vm.LoadEventDetailAsync("e1");

        Assert.NotNull(vm.SelectedEvent);
        Assert.Equal("e1", vm.SelectedEvent.EventId);
        Assert.Equal("detail desc", vm.SelectedEvent.Description);
    }

    [Fact]
    public async Task LoadEventDetailAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.EventDetailAsync("e1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("detail error"));

        var vm = new EventsViewModel(kernelClient.Object);
        await vm.LoadEventDetailAsync("e1");

        Assert.True(vm.HasError);
        Assert.Contains("detail error", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadEventsByMomentAsync_ShouldCallEventsByMomentAsyncAndUpdateEvents()
    {
        var kernelClient = new Mock<IKernelClient>();
        var events = new List<EventInfo>
        {
            new("m1", "momentType", "moment desc", "src1", DateTimeOffset.UtcNow, null),
        };
        kernelClient.Setup(k => k.EventsByMomentAsync("moment1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var vm = new EventsViewModel(kernelClient.Object);
        await vm.LoadEventsByMomentAsync("moment1");

        Assert.Single(vm.Events);
        Assert.Single(vm.FilteredEvents);
        Assert.Equal("m1", vm.Events[0].EventId);
    }

    [Fact]
    public async Task LoadEventsByMomentAsync_WhenApiThrows_ShouldSetError()
    {
        var kernelClient = new Mock<IKernelClient>();
        kernelClient.Setup(k => k.EventsByMomentAsync("moment1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("moment error"));

        var vm = new EventsViewModel(kernelClient.Object);
        await vm.LoadEventsByMomentAsync("moment1");

        Assert.True(vm.HasError);
        Assert.Contains("moment error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void SetTypeFilter_ShouldFilterEvents()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new EventsViewModel(kernelClient.Object);
        vm.Events.Add(new EventInfo("e1", "info", "desc1", "src1", DateTimeOffset.UtcNow, null));
        vm.Events.Add(new EventInfo("e2", "warning", "desc2", "src2", DateTimeOffset.UtcNow, null));
        vm.Events.Add(new EventInfo("e3", "info", "desc3", "src3", DateTimeOffset.UtcNow, null));

        vm.SetTypeFilter("info");

        Assert.Equal(2, vm.FilteredEvents.Count);
        Assert.All(vm.FilteredEvents, e => Assert.Equal("info", e.Type));
    }

    [Fact]
    public void SetTypeFilter_WhenEmpty_ShouldShowAllEvents()
    {
        var kernelClient = new Mock<IKernelClient>();
        var vm = new EventsViewModel(kernelClient.Object);

        vm.SetTypeFilter("");

        Assert.Empty(vm.FilteredEvents);
    }

    [Fact]
    public void ClearError_ShouldClearErrorMessage()
    {
        var vm = new EventsViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearError();

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void ClearErrorCommand_ShouldClearErrorMessage()
    {
        var vm = new EventsViewModel();
        vm.ErrorMessage = "some error";

        vm.ClearErrorCommand.Execute(null);

        Assert.False(vm.HasError);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void LoadRecentCommand_ShouldExist()
    {
        var vm = new EventsViewModel();
        Assert.NotNull(vm.LoadRecentCommand);
    }

    [Fact]
    public void LoadByMomentCommand_ShouldExist()
    {
        var vm = new EventsViewModel();
        Assert.NotNull(vm.LoadByMomentCommand);
    }
}
