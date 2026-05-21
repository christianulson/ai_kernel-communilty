using KrnlAI.Desktop.App.ViewModels;
using Xunit;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class CausalGraphViewModelTests
{
    [Fact] public void DefaultTab_ShouldBeQuery()
    {
        var vm = new CausalGraphViewModel();
        Assert.Equal("query", vm.Tab);
    }
    [Fact] public void IsSearching_Default_ShouldBeFalse() => Assert.False(new CausalGraphViewModel().IsSearching);
    [Fact] public void IsPredicting_Default_ShouldBeFalse() => Assert.False(new CausalGraphViewModel().IsPredicting);
    [Fact] public void SetQueryTab_ShouldSwitch()
    {
        var vm = new CausalGraphViewModel();
        vm.Tab = "predict";
        vm.SetQueryTabCommand.Execute(null);
        Assert.Equal("query", vm.Tab);
    }
    [Fact] public void SetPredictTab_ShouldSwitch()
    {
        var vm = new CausalGraphViewModel();
        vm.SetPredictTabCommand.Execute(null);
        Assert.Equal("predict", vm.Tab);
    }
}
