namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class VersionsViewModelTests
{
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new VersionsViewModel().IsLoading);
    [Fact] public void Versions_Default_ShouldBeNull() => Assert.Null(new VersionsViewModel().Versions);
    [Fact] public void Contracts_Default_ShouldBeEmpty() => Assert.Empty(new VersionsViewModel().Contracts ?? []);
}
