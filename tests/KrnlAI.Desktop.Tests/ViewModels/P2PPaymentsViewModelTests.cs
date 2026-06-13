using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class P2PPaymentsViewModelTests
{
    [Fact]
    public void EarnedFormatted_Default_ShouldBeZero()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.StartsWith("$0", vm.EarnedFormatted);
    }

    [Fact]
    public void SpentFormatted_Default_ShouldBeZero()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.StartsWith("$0", vm.SpentFormatted);
    }

    [Fact]
    public void PendingFormatted_Default_ShouldBeZero()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.StartsWith("$0", vm.PendingFormatted);
    }

    [Fact]
    public void SelectedMode_Default_ShouldBeTrackOnly()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.Equal("TrackOnly", vm.SelectedMode);
    }

    [Fact]
    public void ModeDescription_ForTrackOnly_ShouldReturnExpected()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.Contains("rastreio", vm.ModeDescription);
    }

    [Fact]
    public void ModeDescription_ForFree_ShouldReturnExpected()
    {
        var vm = new P2PPaymentsViewModel();
        vm.SelectedMode = "Free";
        Assert.Contains("gratuito", vm.ModeDescription);
    }

    [Fact]
    public void ModeDescription_ForSettleOnChain_ShouldReturnExpected()
    {
        var vm = new P2PPaymentsViewModel();
        vm.SelectedMode = "SettleOnChain";
        Assert.Contains("on-chain", vm.ModeDescription);
    }

    [Fact]
    public void AvailableModes_ShouldContainThreeOptions()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.Equal(3, vm.AvailableModes.Count);
        Assert.Contains("Free", vm.AvailableModes);
        Assert.Contains("TrackOnly", vm.AvailableModes);
        Assert.Contains("SettleOnChain", vm.AvailableModes);
    }

    [Fact]
    public void Receipts_Default_ShouldBeEmpty()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.Empty(vm.Receipts);
    }

    [Fact]
    public void Batches_Default_ShouldBeEmpty()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.Empty(vm.Batches);
    }

    [Fact]
    public void PendingReceiptCount_Default_ShouldBeZero()
    {
        var vm = new P2PPaymentsViewModel();
        Assert.Equal(0, vm.PendingReceiptCount);
    }
}
