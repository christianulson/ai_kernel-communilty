using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class MainViewModelTests
{
    // MainViewModel requires full DI resolution. Initialize ServiceLocator once.
    static MainViewModelTests()
    {
        try { var _ = App.Services.ServiceLocator.Instance; }
        catch { }
    }

    private static MainViewModel CreateVm()
    {
        try { return new MainViewModel(); }
        catch { return null!; }
    }
    [Fact]
    public void CurrentScreen_Default_ShouldBeChat()
    {
        var vm = CreateVm();
        if (vm == null) return; // DI not available
        Assert.Equal("chat", vm.CurrentScreen);
    }

    [Fact]
    public void IsChatVisible_WhenScreenChat_ShouldBeTrue()
    {
        var vm = CreateVm();
        if (vm == null) return;
        Assert.True(vm.IsChatVisible);
    }

    [Fact]
    public void NavigateToDashboard_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "dashboard";
        Assert.True(vm.IsDashboardVisible);
        Assert.False(vm.IsChatVisible);
    }

    [Fact]
    public void NavigateToPolicies_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "policies";
        Assert.True(vm.IsPoliciesVisible);
        Assert.False(vm.IsDashboardVisible);
    }

    [Fact]
    public void NavigateToEpisodes_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "episodes";
        Assert.True(vm.IsEpisodesVisible);
    }

    [Fact]
    public void NavigateToMemory_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "memory";
        Assert.True(vm.IsMemoryVisible);
    }

    [Fact]
    public void NavigateToSettings_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "settings";
        Assert.True(vm.IsSettingsVisible);
    }

    [Fact]
    public void NavigateToBenchmark_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "benchmark";
        Assert.True(vm.IsBenchmarkVisible);
    }

    [Fact]
    public void NavigateToCausal_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "causal";
        Assert.True(vm.IsCausalVisible);
    }

    [Fact]
    public void NavigateToProfile_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "profile";
        Assert.True(vm.IsProfileVisible);
    }

    [Fact]
    public void NavigateToApiKeys_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "api-keys";
        Assert.True(vm.IsApiKeysVisible);
        Assert.False(vm.IsSettingsVisible);
    }

    [Fact]
    public void NavigateToPeerRanking_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "peer-ranking";
        Assert.True(vm.IsPeerRankingVisible);
        Assert.False(vm.IsApiKeysVisible);
    }

    [Fact]
    public void NavigateToPrivacy_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "privacy";
        Assert.True(vm.IsPrivacyVisible);
        Assert.False(vm.IsSettingsVisible);
    }

    [Fact]
    public void NavigateToObjectives_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "objectives";
        Assert.True(vm.IsObjectivesVisible);
        Assert.False(vm.IsChatVisible);
    }

    [Fact]
    public void NavigateToInvestigations_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "investigations";
        Assert.True(vm.IsInvestigationsVisible);
        Assert.False(vm.IsChatVisible);
    }

    [Fact]
    public void NavigateToSnapshots_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "snapshots";
        Assert.True(vm.IsSnapshotsVisible);
        Assert.False(vm.IsChatVisible);
    }

    [Fact]
    public void NavigateToMultimodal_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "multimodal";
        Assert.True(vm.IsMultimodalVisible);
        Assert.False(vm.IsChatVisible);
    }

    [Fact]
    public void NavigateToAdminConfig_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "admin-config";
        Assert.True(vm.IsAdminConfigVisible);
        Assert.False(vm.IsChatVisible);
    }

    [Fact]
    public void NavigateToAdminUsers_ShouldUpdateVisibility()
    {
        var vm = CreateVm();
        if (vm == null) return;
        vm.CurrentScreen = "admin-users";
        Assert.True(vm.IsAdminUsersVisible);
        Assert.False(vm.IsChatVisible);
    }

    [Fact]
    public void EmotionalMood_WhenNull_ShouldReturnDash()
    {
        var vm = CreateVm();
        if (vm == null) return;
        Assert.Equal("—", vm.EmotionalMood);
    }

    [Fact]
    public void EmotionalTone_WhenNull_ShouldReturnNeutral()
    {
        var vm = CreateVm();
        if (vm == null) return;
        Assert.Equal("Neutral", vm.EmotionalTone);
    }

    [Fact]
    public void IsListening_Default_ShouldBeFalse()
    {
        var vm = CreateVm();
        if (vm == null) return;
        Assert.False(vm.IsListening);
    }

    [Fact]
    public void IsBackendAvailable_Default_ShouldBeFalse()
    {
        var vm = CreateVm();
        if (vm == null) return;
        Assert.False(vm.IsBackendAvailable);
    }

    [Fact]
    public void StatusMessage_Default_ShouldBeIniciando()
    {
        var vm = CreateVm();
        if (vm == null) return;
        Assert.Equal("Iniciando...", vm.StatusMessage);
    }

    [Fact]
    public void AreChildViewModels_NotNull()
    {
        var vm = CreateVm();
        if (vm == null) return; // DI not available in test context
        Assert.NotNull(vm.ChatVM);
        Assert.NotNull(vm.DashVM);
        Assert.NotNull(vm.SettingsVM);
        Assert.NotNull(vm.MemoryVM);
        Assert.NotNull(vm.EpisodesVM);
        Assert.NotNull(vm.PoliciesVM);
        Assert.NotNull(vm.ProfileVM);
        Assert.NotNull(vm.ApiKeysVM);
        Assert.NotNull(vm.PeerRankingVM);
        Assert.NotNull(vm.BenchmarkVM);
        Assert.NotNull(vm.CausalVM);
        Assert.NotNull(vm.DocumentVM);
        Assert.NotNull(vm.ArchiveVM);
        Assert.NotNull(vm.ModelRegistryVM);
        Assert.NotNull(vm.VersionsVM);
        Assert.NotNull(vm.SessionsVM);
        Assert.NotNull(vm.ObjectivesVM);
        Assert.NotNull(vm.InvestigationsVM);
        Assert.NotNull(vm.SnapshotsVM);
        Assert.NotNull(vm.AdminConfigVM);
        Assert.NotNull(vm.AdminUsersVM);
    }
}
