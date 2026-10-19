namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class WelcomeWizardViewModelTests
{
    [Fact]
    public void IsLocalModeSelected_Default_ShouldBeTrue()
    {
        var vm = new WelcomeWizardViewModel();
        Assert.True(vm.IsLocalModeSelected);
    }

    [Fact]
    public void IsCloudModeSelected_Default_ShouldBeFalse()
    {
        var vm = new WelcomeWizardViewModel();
        Assert.False(vm.IsCloudModeSelected);
    }

    [Fact]
    public void SelectCloudMode_ShouldUnselectLocal()
    {
        var vm = new WelcomeWizardViewModel();
        vm.IsCloudModeSelected = true;
        Assert.True(vm.IsCloudModeSelected);
        Assert.False(vm.IsLocalModeSelected);
    }

    [Fact]
    public void SelectLocalMode_ShouldUnselectCloud()
    {
        var vm = new WelcomeWizardViewModel();
        vm.IsCloudModeSelected = true;
        vm.IsLocalModeSelected = true;
        Assert.True(vm.IsLocalModeSelected);
        Assert.False(vm.IsCloudModeSelected);
    }

    [Fact]
    public void CurrentStep_Default_ShouldBe1()
    {
        var vm = new WelcomeWizardViewModel();
        Assert.Equal(1, vm.CurrentStep);
    }

    [Fact]
    public void Next_ShouldAdvanceStep()
    {
        var vm = new WelcomeWizardViewModel();
        vm.NextStepCommand.Execute(null);
        Assert.Equal(2, vm.CurrentStep);
        Assert.True(vm.IsStep2Visible);
    }

    [Fact]
    public void Previous_ShouldGoBack()
    {
        var vm = new WelcomeWizardViewModel();
        vm.NextStepCommand.Execute(null);
        vm.PreviousStepCommand.Execute(null);
        Assert.Equal(1, vm.CurrentStep);
        Assert.True(vm.IsStep1Visible);
    }

    [Fact]
    public void IsLastStep_OnStep3_ShouldBeTrue()
    {
        var vm = new WelcomeWizardViewModel();
        vm.NextStepCommand.Execute(null);
        vm.NextStepCommand.Execute(null);
        Assert.True(vm.IsLastStep);
        Assert.Equal("Começar!", vm.NextButtonText);
    }

    [Fact]
    public void IsNotFirstStep_Initially_ShouldBeFalse()
    {
        var vm = new WelcomeWizardViewModel();
        Assert.False(vm.IsNotFirstStep);
    }

    [Fact]
    public void Reset_ShouldRestoreStep1()
    {
        var vm = new WelcomeWizardViewModel();
        vm.NextStepCommand.Execute(null);
        vm.Reset();
        Assert.Equal(1, vm.CurrentStep);
    }

    [Fact]
    public void StepProgress_ShouldReflectCurrentStep()
    {
        var vm = new WelcomeWizardViewModel();
        Assert.True(vm.Step1Active);
        Assert.False(vm.Step2Active);
        Assert.False(vm.Step3Active);

        vm.NextStepCommand.Execute(null);
        Assert.True(vm.Step1Active);
        Assert.True(vm.Step2Active);
        Assert.False(vm.Step3Active);
    }

    [Fact]
    public void Previous_AtStep1_ShouldNotGoBelow1()
    {
        var vm = new WelcomeWizardViewModel();
        vm.PreviousStepCommand.Execute(null);
        Assert.Equal(1, vm.CurrentStep);
    }
}
