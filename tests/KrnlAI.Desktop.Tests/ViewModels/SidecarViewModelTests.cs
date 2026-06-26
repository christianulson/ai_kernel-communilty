namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class SidecarViewModelTests
{
    [Fact]
    public void Mode_Default_ShouldBeCommunity()
    {
        var vm = new SidecarViewModel();
        Assert.Equal("community", vm.Mode);
    }

    [Fact]
    public void ModeLabel_ForCommunity_ShouldContainModo()
    {
        var vm = new SidecarViewModel();
        Assert.Contains("Modo", vm.ModeLabel);
        Assert.Contains("community", vm.ModeLabel);
    }

    [Fact]
    public void ModeDescription_ForEnterprise_ShouldMentionAuth()
    {
        var vm = new SidecarViewModel();
        vm.Mode = "enterprise";
        Assert.Contains("Autenticação", vm.ModeDescription);
    }

    [Fact]
    public void ModeDescription_ForCommunity_ShouldMentionLocal()
    {
        var vm = new SidecarViewModel();
        Assert.Contains("local", vm.ModeDescription);
    }

    [Fact]
    public void AuthEndpoint_ShouldRoundTrip()
    {
        var vm = new SidecarViewModel();
        vm.AuthEndpoint = "http://auth.example.com";
        Assert.Equal("http://auth.example.com", vm.AuthEndpoint);
    }

    [Fact]
    public void GatewayEndpoint_ShouldRoundTrip()
    {
        var vm = new SidecarViewModel();
        vm.GatewayEndpoint = "http://gw.example.com";
        Assert.Equal("http://gw.example.com", vm.GatewayEndpoint);
    }

    [Fact]
    public void TenantId_ShouldRoundTrip()
    {
        var vm = new SidecarViewModel();
        vm.TenantId = "tenant-42";
        Assert.Equal("tenant-42", vm.TenantId);
    }

    [Fact]
    public void StatusMessage_Default_ShouldBePronto()
    {
        var vm = new SidecarViewModel();
        Assert.Equal("Pronto.", vm.StatusMessage);
    }

    [Fact]
    public void ResetToCommunity_ShouldClearEnterpriseFields()
    {
        var vm = new SidecarViewModel();
        vm.Mode = "enterprise";
        vm.AuthEndpoint = "http://auth.ex.com";
        vm.GatewayEndpoint = "http://gw.ex.com";
        vm.ApiKey = "secret";
        vm.TenantId = "t1";

        vm.ResetToCommunityCommand.Execute(null);

        Assert.Equal("community", vm.Mode);
        Assert.Equal("", vm.AuthEndpoint);
        Assert.Equal("", vm.GatewayEndpoint);
        Assert.Equal("", vm.ApiKey);
        Assert.Equal("", vm.TenantId);
    }
}
