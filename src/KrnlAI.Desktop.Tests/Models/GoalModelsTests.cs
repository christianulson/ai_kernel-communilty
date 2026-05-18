namespace KrnlAI.Desktop.Tests.Models;

public class GoalInfoTests
{
    [Fact]
    public void GoalInfo_ShouldCreateWithCorrectProperties()
    {
        var now = DateTime.UtcNow;
        var info = new Core.Models.GoalInfo("g1", "Do something", "active", 3, now, null, now.AddDays(7), 0.5, 5, 2);

        Assert.Equal("g1", info.GoalId);
        Assert.Equal("Do something", info.Description);
        Assert.Equal("active", info.Status);
        Assert.Equal(3, info.Priority);
        Assert.Equal(now, info.CreatedAt);
        Assert.Null(info.CompletedAt);
        Assert.Equal(now.AddDays(7), info.Deadline);
        Assert.Equal(0.5, info.SuccessRate);
        Assert.Equal(5, info.SubGoalCount);
        Assert.Equal(2, info.CompletedSubGoals);
    }

    [Fact]
    public void GoalInfo_ShouldAllowCompletedGoal()
    {
        var info = new Core.Models.GoalInfo("g1", "Done", "completed", 1, DateTime.UtcNow, DateTime.UtcNow, null, 1.0, 3, 3);
        Assert.Equal("completed", info.Status);
        Assert.NotNull(info.CompletedAt);
        Assert.Equal(1.0, info.SuccessRate);
    }
}

public class GoalDetailsTests
{
    [Fact]
    public void GoalDetails_ShouldIncludeSubGoals()
    {
        var subGoals = new List<Core.Models.SubGoal>
        {
            new("s1", "Step 1", true),
            new("s2", "Step 2", false)
        };
        var details = new Core.Models.GoalDetails("g1", "Goal", "active", 2, DateTime.UtcNow, null, null, null, subGoals, null);

        Assert.Equal(2, details.SubGoals!.Count);
        Assert.True(details.SubGoals[0].Completed);
        Assert.False(details.SubGoals[1].Completed);
    }

    [Fact]
    public void GoalDetails_ShouldIncludeCycles()
    {
        var cycles = new List<Core.Models.GoalCycle>
        {
            new("init", "done", 100, DateTime.UtcNow, "g1")
        };
        var details = new Core.Models.GoalDetails("g1", "Goal", "active", 2, DateTime.UtcNow, null, null, null, null, cycles);

        Assert.Single(details.Cycles!);
        Assert.Equal("init", details.Cycles![0].Action);
    }
}

public class SubGoalTests
{
    [Fact]
    public void SubGoal_ShouldCreate()
    {
        var sg = new Core.Models.SubGoal("s1", "Do this", true);
        Assert.Equal("s1", sg.Id);
        Assert.Equal("Do this", sg.Description);
        Assert.True(sg.Completed);
    }
}

public class GoalCycleTests
{
    [Fact]
    public void GoalCycle_ShouldCreate()
    {
        var now = DateTime.UtcNow;
        var cycle = new Core.Models.GoalCycle("action", "done", 200, now, "g1");

        Assert.Equal("action", cycle.Action);
        Assert.Equal(200, cycle.DurationMs);
        Assert.Equal(now, cycle.Timestamp);
    }
}

public class CreateGoalRequestTests
{
    [Fact]
    public void CreateGoalRequest_ShouldHaveDefaultPriority()
    {
        var req = new Core.Models.CreateGoalRequest("Do something");
        Assert.Equal("Do something", req.Description);
        Assert.Equal(3, req.Priority);
        Assert.Null(req.Deadline);
    }

    [Fact]
    public void CreateGoalRequest_ShouldAllowCustomValues()
    {
        var deadline = DateTime.UtcNow.AddDays(7);
        var req = new Core.Models.CreateGoalRequest("Important", 1, deadline);
        Assert.Equal(1, req.Priority);
        Assert.Equal(deadline, req.Deadline);
    }
}

public class GoalListResponseTests
{
    [Fact]
    public void GoalListResponse_ShouldStore()
    {
        var goals = new List<Core.Models.GoalInfo> { new("g1", "Test", "active", 1, DateTime.UtcNow, null, null, null, 0, 0) };
        var resp = new Core.Models.GoalListResponse(goals, 1);

        Assert.Single(resp.Goals);
        Assert.Equal(1, resp.TotalCount);
    }

    [Fact]
    public void GoalListResponse_ShouldAllowEmpty()
    {
        var resp = new Core.Models.GoalListResponse(new List<Core.Models.GoalInfo>(), 0);
        Assert.Empty(resp.Goals);
        Assert.Equal(0, resp.TotalCount);
    }
}
