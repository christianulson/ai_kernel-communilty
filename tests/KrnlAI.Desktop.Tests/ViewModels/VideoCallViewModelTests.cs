using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class VideoCallViewModelTests
{
    [Fact] public void IsInVideoCall_Default_ShouldBeFalse() => Assert.False(new VideoCallViewModel().IsInVideoCall);
    [Fact] public void IsVideoCallMuted_Default_ShouldBeFalse() => Assert.False(new VideoCallViewModel().IsVideoCallMuted);
    [Fact] public void VideoCallState_Default_ShouldBeIdle() => Assert.Equal("Idle", new VideoCallViewModel().VideoCallState);
}
