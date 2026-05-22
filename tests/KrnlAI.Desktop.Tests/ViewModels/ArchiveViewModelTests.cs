using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ArchiveViewModelTests
{
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new ArchiveViewModel().IsLoading);
    [Fact] public void Stats_Default_ShouldBeNull() => Assert.Null(new ArchiveViewModel().Stats);
}
