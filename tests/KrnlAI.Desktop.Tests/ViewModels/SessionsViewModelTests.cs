using KrnlAI.Desktop.App.ViewModels;
using Xunit;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class SessionsViewModelTests
{
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new SessionsViewModel().IsLoading);
    [Fact] public void Shares_Default_ShouldBeEmpty() => Assert.Empty(new SessionsViewModel().Shares);
}
