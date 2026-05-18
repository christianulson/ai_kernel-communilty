namespace KrnlAI.Desktop.Tests.Models;

public class EpisodeInfoTests
{
    [Fact]
    public void EpisodeInfo_ShouldCreateWithCorrectProperties()
    {
        var now = DateTime.UtcNow;
        var info = new Core.Models.EpisodeInfo("e1", "g1", "completed", now, now, 500, "success", 0.95);

        Assert.Equal("e1", info.Id);
        Assert.Equal("g1", info.GoalId);
        Assert.Equal("completed", info.Status);
        Assert.Equal(now, info.CreatedAt);
        Assert.Equal(now, info.FinishedAt);
        Assert.Equal(500, info.DurationMs);
        Assert.Equal("success", info.Outcome);
        Assert.Equal(0.95, info.SuccessRate);
    }

    [Fact]
    public void EpisodeInfo_ShouldAllowNullOptionalFields()
    {
        var info = new Core.Models.EpisodeInfo("e1", "g1", "running", DateTime.UtcNow, null, null, null, null);
        Assert.Null(info.FinishedAt);
        Assert.Null(info.DurationMs);
        Assert.Null(info.Outcome);
        Assert.Null(info.SuccessRate);
    }
}

public class EpisodeDetailsTests
{
    [Fact]
    public void EpisodeDetails_ShouldExtendEpisodeInfo()
    {
        var steps = new List<Core.Models.EpisodeStep> { new(0, "Step 1", "Detail", DateTime.UtcNow, DateTime.UtcNow, 100, true, null) };
        var detail = new Core.Models.EpisodeDetails("e1", "g1", "completed", DateTime.UtcNow, null, 500, "ok", 0.9, "summary", steps);

        Assert.Equal("summary", detail.Summary);
        Assert.Single(detail.Steps!);
        Assert.Equal("Step 1", detail.Steps![0].Label);
    }

    [Fact]
    public void EpisodeDetails_ShouldAllowNullSteps()
    {
        var detail = new Core.Models.EpisodeDetails("e1", "g1", "completed", DateTime.UtcNow, null, null, null, null, null, null);
        Assert.Null(detail.Steps);
    }
}

public class EpisodeStepTests
{
    [Fact]
    public void EpisodeStep_ShouldCreateWithCorrectProperties()
    {
        var step = new Core.Models.EpisodeStep(0, "Init", "Starting", DateTime.UtcNow, DateTime.UtcNow, 50, true, null);

        Assert.Equal(0, step.StepIndex);
        Assert.Equal("Init", step.Label);
        Assert.True(step.Ok);
        Assert.Null(step.Error);
    }

    [Fact]
    public void EpisodeStep_ShouldSupportErrorState()
    {
        var step = new Core.Models.EpisodeStep(0, "Init", "Failed", DateTime.UtcNow, null, null, false, "timeout");
        Assert.False(step.Ok);
        Assert.Equal("timeout", step.Error);
    }
}

public class EpisodeSearchTests
{
    [Fact]
    public void EpisodeSearchRequest_ShouldHaveDefaults()
    {
        var req = new Core.Models.EpisodeSearchRequest();
        Assert.Equal(1, req.Page);
        Assert.Equal(20, req.PageSize);
        Assert.Null(req.Query);
        Assert.Null(req.Status);
    }

    [Fact]
    public void EpisodeSearchRequest_ShouldAllowCustomValues()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var req = new Core.Models.EpisodeSearchRequest("test", "g1", "failed", from, null, 2, 10);
        Assert.Equal("test", req.Query);
        Assert.Equal("failed", req.Status);
        Assert.Equal(from, req.FromDate);
    }

    [Fact]
    public void EpisodeSearchResult_ShouldStoreValues()
    {
        var episodes = new List<Core.Models.EpisodeInfo> { new("e1", "g1", "completed", DateTime.UtcNow, null, null, null, null) };
        var result = new Core.Models.EpisodeSearchResult(episodes, 1, 1, 20);

        Assert.Single(result.Episodes);
        Assert.Equal(1, result.TotalCount);
    }
}
