using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ModelRegistryViewModelTests
{
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new ModelRegistryViewModel().IsLoading);
    [Fact] public void Models_Default_ShouldBeEmpty() => Assert.Empty(new ModelRegistryViewModel().Models);
}
