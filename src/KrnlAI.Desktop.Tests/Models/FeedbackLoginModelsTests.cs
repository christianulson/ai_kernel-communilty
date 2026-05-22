namespace KrnlAI.Desktop.Tests.Models;

public class FeedbackRequestTests
{
    [Fact]
    public void FeedbackRequest_ShouldCreate()
    {
        var req = new Core.Models.FeedbackRequest("ep1", 5, "Great!", "quality");
        Assert.Equal("ep1", req.EpisodeId);
        Assert.Equal(5, req.Rating);
        Assert.Equal("Great!", req.Comment);
        Assert.Equal("quality", req.Category);
    }

    [Fact]
    public void FeedbackRequest_ShouldAllowNullComment()
    {
        var req = new Core.Models.FeedbackRequest("ep1", 3, null, null);
        Assert.Null(req.Comment);
        Assert.Null(req.Category);
    }
}

public class FeedbackResponseTests
{
    [Fact]
    public void FeedbackResponse_ShouldStoreSuccess()
    {
        var resp = new Core.Models.FeedbackResponse(true, "fb1", "ok");
        Assert.True(resp.Success);
        Assert.Equal("fb1", resp.FeedbackId);
        Assert.Equal("ok", resp.Message);
    }

    [Fact]
    public void FeedbackResponse_ShouldStoreFailure()
    {
        var resp = new Core.Models.FeedbackResponse(false, null, "error");
        Assert.False(resp.Success);
        Assert.Null(resp.FeedbackId);
    }
}

public class FeedbackAverageTests
{
    [Fact]
    public void FeedbackAverage_ShouldCreate()
    {
        var avg = new Core.Models.FeedbackAverage(10, 4.2, 1, 1, 2, 3, 3);
        Assert.Equal(10, avg.TotalFeedbacks);
        Assert.Equal(4.2, avg.AverageRating);
        Assert.Equal(3, avg.Rating5Count);
    }
}

public class LoginModelTests
{
    [Fact]
    public void LoginRequest_ShouldCreate()
    {
        var req = new Core.Models.LoginRequest("admin@test.com", "pass");
        Assert.Equal("admin@test.com", req.Email);
        Assert.Equal("pass", req.Password);
    }

    [Fact]
    public void LoginResponse_ShouldStoreSuccess()
    {
        var resp = new Core.Models.LoginResponse(true, "token123", "ok", "admin@test.com", "refresh-abc");
        Assert.True(resp.Success);
        Assert.Equal("token123", resp.Token);
        Assert.Equal("admin@test.com", resp.Username);
        Assert.Equal("refresh-abc", resp.RefreshToken);
    }

    [Fact]
    public void LoginResponse_ShouldStoreFailure()
    {
        var resp = new Core.Models.LoginResponse(false, null, "invalid credentials");
        Assert.False(resp.Success);
        Assert.Null(resp.Token);
        Assert.Equal("invalid credentials", resp.Message);
    }
}

public class AuthSettingsTests
{
    [Fact]
    public void AuthSettings_ShouldCreate()
    {
        var auth = new Core.Models.AuthSettings("token", "user", DateTime.UtcNow.AddDays(1));
        Assert.Equal("token", auth.Token);
        Assert.Equal("user", auth.Username);
        Assert.NotNull(auth.ExpiresAt);
    }
}

public class TransportStepTests
{
    [Fact]
    public void TransportStep_ShouldCreate()
    {
        var step = new Core.Models.TransportStep("Init", "Starting", true, 200);
        Assert.Equal("Init", step.Label);
        Assert.True(step.Ok);
        Assert.Equal(200, step.Status);
    }

    [Fact]
    public void TransportStep_ShouldAllowNullStatus()
    {
        var step = new Core.Models.TransportStep("Init", "Pending", false, null);
        Assert.False(step.Ok);
        Assert.Null(step.Status);
    }
}
