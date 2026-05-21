using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Models;
using Xunit;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class DashboardViewModelTests
{
    [Fact]
    public void EmotionalMood_WhenNull_ShouldReturnDash()
    {
        var vm = new DashboardViewModel();
        // EmotionalState is null initially
        Assert.Equal("—", vm.EmotionalMood);
    }

    [Fact]
    public void IsLoading_Default_ShouldBeFalse()
    {
        var vm = new DashboardViewModel();
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void HasError_WhenNoError_ShouldBeFalse()
    {
        var vm = new DashboardViewModel();
        Assert.False(vm.HasError);
    }

    [Fact]
    public void ErrorMessage_WhenSet_ShouldUpdateHasError()
    {
        var vm = new DashboardViewModel();
        vm.ErrorMessage = "error";
        Assert.True(vm.HasError);
        vm.ErrorMessage = "";
        Assert.False(vm.HasError);
    }

    [Fact]
    public void NewGoalDescription_ShouldRoundTrip()
    {
        var vm = new DashboardViewModel();
        vm.NewGoalDescription = "test goal";
        Assert.Equal("test goal", vm.NewGoalDescription);
    }

    [Fact]
    public void NewGoalPriority_Default_ShouldBe3()
    {
        var vm = new DashboardViewModel();
        Assert.Equal(3, vm.NewGoalPriority);
    }

    [Fact]
    public void IsGoalCreateVisible_Default_ShouldBeFalse()
    {
        var vm = new DashboardViewModel();
        Assert.False(vm.IsGoalCreateVisible);
    }

    [Fact]
    public void ShowCreateGoal_ShouldSetVisible()
    {
        var vm = new DashboardViewModel();
        vm.ShowCreateGoalCommand.Execute(null);
        Assert.True(vm.IsGoalCreateVisible);
    }

    [Fact]
    public void HideCreateGoal_ShouldClearDescriptionAndHide()
    {
        var vm = new DashboardViewModel();
        vm.NewGoalDescription = "test";
        vm.IsGoalCreateVisible = true;
        vm.HideCreateGoalCommand.Execute(null);
        Assert.False(vm.IsGoalCreateVisible);
        Assert.Equal("", vm.NewGoalDescription);
    }

    [Fact]
    public void Status_Default_ShouldBeEmpty()
    {
        var vm = new DashboardViewModel();
        Assert.Equal("", vm.Status);
    }

    [Fact]
    public void EmotionalTone_WhenNull_ShouldReturnNeutral()
    {
        var vm = new DashboardViewModel();
        Assert.Equal("Neutral", vm.EmotionalTone);
    }

    [Fact]
    public void EmotionalMotive_WhenNull_ShouldReturnDash()
    {
        var vm = new DashboardViewModel();
        Assert.Equal("—", vm.EmotionalMotive);
    }
}
