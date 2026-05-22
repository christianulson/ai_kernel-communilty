using KrnlAI.Desktop.App.ViewModels;
namespace KrnlAI.Desktop.Tests.ViewModels;
public sealed class DocumentViewModelTests
{
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new DocumentViewModel().IsLoading);
}
