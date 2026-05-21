using KrnlAI.Desktop.App.ViewModels;
using Xunit;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class PoliciesViewModelTests
{
    [Fact] public void IsLoading_Default_ShouldBeFalse() => Assert.False(new PoliciesViewModel().IsLoading);
    [Fact] public void PolicyList_Default_ShouldBeEmpty() => Assert.Empty(new PoliciesViewModel().PolicyList);
    [Fact] public void PolicyDomains_ShouldContainDefault() => Assert.Contains("general", new PoliciesViewModel().PolicyDomains);
}
