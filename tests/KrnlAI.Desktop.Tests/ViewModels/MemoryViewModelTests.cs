namespace KrnlAI.Desktop.Tests.ViewModels;
public sealed class MemoryViewModelTests
{
    [Fact] public void DefaultTab_ShouldBeSearch()
    {
        var vm = new MemoryViewModel();
        Assert.Equal("search", vm.MemoryTab);
    }
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new MemoryViewModel().IsLoading);
}
