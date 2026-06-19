namespace KrnlAI.Desktop.Tests.Models;

public class PolicyInfoTests
{
    [Fact]
    public void PolicyInfo_ShouldCreateWithCorrectProperties()
    {
        var now = DateTime.UtcNow;
        var info = new Core.Models.PolicyInfo("p1", "Test Policy", "http", "1.0", now, now, true);

        Assert.Equal("p1", info.Id);
        Assert.Equal("Test Policy", info.Name);
        Assert.Equal("http", info.Domain);
        Assert.Equal("1.0", info.Version);
        Assert.Equal(now, info.CreatedAt);
        Assert.Equal(now, info.UpdatedAt);
        Assert.True(info.IsActive);
    }

    [Fact]
    public void PolicyInfo_ShouldAllowNullUpdatedAt()
    {
        var info = new Core.Models.PolicyInfo("p1", "Test", "http", "1.0", DateTime.UtcNow, null, true);
        Assert.Null(info.UpdatedAt);
    }
}

public class PolicyDetailsTests
{
    [Fact]
    public void PolicyDetails_ShouldExtendPolicyInfo()
    {
        var details = new Core.Models.PolicyDetails("p1", "Test", "http", "1.0", "content", DateTime.UtcNow, null, true, null);

        Assert.Equal("p1", details.Id);
        Assert.Equal("content", details.Content);
        Assert.Null(details.Versions);
    }

    [Fact]
    public void PolicyDetails_ShouldIncludeVersions()
    {
        var versions = new List<Core.Models.PolicyVersion> { new("1.0", DateTime.UtcNow, "admin", "First version") };
        var details = new Core.Models.PolicyDetails("p1", "Test", "http", "1.0", "content", DateTime.UtcNow, null, true, versions);

        Assert.Single(details.Versions!);
        Assert.Equal("admin", details.Versions![0].CreatedBy);
    }
}

public class PolicyVersionTests
{
    [Fact]
    public void PolicyVersion_ShouldCreateWithCorrectProperties()
    {
        var now = DateTime.UtcNow;
        var v = new Core.Models.PolicyVersion("1.1", now, "operator", "Fixed bug");

        Assert.Equal("1.1", v.Version);
        Assert.Equal(now, v.CreatedAt);
        Assert.Equal("operator", v.CreatedBy);
        Assert.Equal("Fixed bug", v.ChangeNote);
    }

    [Fact]
    public void PolicyVersion_ShouldAllowNullChangeNote()
    {
        var v = new Core.Models.PolicyVersion("1.1", DateTime.UtcNow, "operator", null);
        Assert.Null(v.ChangeNote);
    }
}

public class PolicyRequestTests
{
    [Fact]
    public void CreatePolicyRequest_ShouldStoreValues()
    {
        var req = new Core.Models.CreatePolicyRequest("New Policy", "memory", "rule content");
        Assert.Equal("New Policy", req.Name);
        Assert.Equal("memory", req.Domain);
        Assert.Equal("rule content", req.Content);
    }

    [Fact]
    public void UpdatePolicyRequest_ShouldStoreValues()
    {
        var req = new Core.Models.UpdatePolicyRequest("Updated", "http", "new content");
        Assert.Equal("Updated", req.Name);
        Assert.Equal("http", req.Domain);
    }
}

public class PolicyListResponseTests
{
    [Fact]
    public void PolicyListResponse_ShouldStoreValues()
    {
        var policies = new List<Core.Models.PolicyInfo> { new("p1", "Test", "http", "1.0", DateTime.UtcNow, null, true) };
        var resp = new Core.Models.PolicyListResponse(policies, 1, 1, 20);

        Assert.Single(resp.Policies);
        Assert.Equal(1, resp.TotalCount);
        Assert.Equal(1, resp.Page);
        Assert.Equal(20, resp.PageSize);
    }

    [Fact]
    public void PolicyListResponse_ShouldAllowEmptyList()
    {
        var resp = new Core.Models.PolicyListResponse([], 0, 1, 20);
        Assert.Empty(resp.Policies);
        Assert.Equal(0, resp.TotalCount);
    }
}
