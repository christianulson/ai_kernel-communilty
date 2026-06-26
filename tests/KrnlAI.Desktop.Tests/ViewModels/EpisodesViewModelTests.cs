namespace KrnlAI.Desktop.Tests.ViewModels;
public sealed class EpisodesViewModelTests
{
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new EpisodesViewModel().IsLoading);
}
