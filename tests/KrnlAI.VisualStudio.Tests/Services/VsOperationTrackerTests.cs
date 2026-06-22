using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

[Trait("Category", "Unit")]
public sealed class VsOperationTrackerTests
{
    [Fact]
    public void Start_ShouldCreateRunningOperation()
    {
        var tracker = new VsOperationTracker();

        using var scope = tracker.Start("test.op", "arg1");

        tracker.History.Should().HaveCount(1);
        var op = tracker.History[0];
        op.Name.Should().Be("test.op");
        op.Arguments.Should().Be("arg1");
        op.State.Should().Be(VsOperationState.Running);
        op.Id.Should().NotBeNullOrEmpty();
        op.Children.Should().BeNull();
    }

    [Fact]
    public void Start_NullArgs_ShouldStoreNull()
    {
        var tracker = new VsOperationTracker();

        using var scope = tracker.Start("noargs");

        tracker.History[0].Arguments.Should().BeNull();
    }

    [Fact]
    public void Dispose_CompletedScope_ShouldMarkCompleted()
    {
        var tracker = new VsOperationTracker();

        using (var scope = tracker.Start("op"))
        {
            scope.SetResult("done");
        }

        var op = tracker.History[0];
        op.State.Should().Be(VsOperationState.Completed);
        op.Result.Should().Be("done");
        op.Error.Should().BeNull();
        op.ElapsedMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void SetError_ShouldMarkFailed()
    {
        var tracker = new VsOperationTracker();

        using (var scope = tracker.Start("op"))
        {
            scope.SetError("something broke");
        }

        var op = tracker.History[0];
        op.State.Should().Be(VsOperationState.Failed);
        op.Error.Should().Be("something broke");
        op.Result.Should().BeNull();
    }

    [Fact]
    public void Dispose_WithoutSetResultOrError_ShouldMarkCompleted()
    {
        var tracker = new VsOperationTracker();

        using (var scope = tracker.Start("op")) { }

        tracker.History[0].State.Should().Be(VsOperationState.Completed);
    }

    [Fact]
    public void StartChild_ShouldNestOperation()
    {
        var tracker = new VsOperationTracker();

        using (var parent = tracker.Start("parent"))
        using (var child = parent.StartChild("child", "child-arg"))
        {
            child.SetResult("child-result");
        }

        var op = tracker.History[0];
        op.Children.Should().NotBeNull();
        op.Children.Should().HaveCount(1);
        op.Children![0].Name.Should().Be("child");
        op.Children[0].Arguments.Should().Be("child-arg");
        op.Children[0].Result.Should().Be("child-result");
        op.Children[0].State.Should().Be(VsOperationState.Completed);
    }

    [Fact]
    public void NestedChild_ShouldTrackTiming()
    {
        var tracker = new VsOperationTracker();

        using (var parent = tracker.Start("parent"))
        {
            using (var childScope = parent.StartChild("child"))
            {
                childScope.SetResult("ok");
            }
            parent.SetResult("done");
        }

        var childOp = tracker.History[0].Children![0];
        childOp.ElapsedMs.Should().BeGreaterThanOrEqualTo(0);
        childOp.StartedAt.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public void MultipleOperations_ShouldAllBeInHistory()
    {
        var tracker = new VsOperationTracker();

        using (var op1 = tracker.Start("first")) { op1.SetResult("a"); }
        using (var op2 = tracker.Start("second")) { op2.SetResult("b"); }
        using (var op3 = tracker.Start("third")) { op3.SetResult("c"); }

        tracker.History.Should().HaveCount(3);
        tracker.History[0].Name.Should().Be("first");
        tracker.History[1].Name.Should().Be("second");
        tracker.History[2].Name.Should().Be("third");
    }

    [Fact]
    public void Clear_ShouldRemoveAllOperations()
    {
        var tracker = new VsOperationTracker();
        using (var op = tracker.Start("op")) { op.SetResult("ok"); }

        tracker.Clear();

        tracker.History.Should().BeEmpty();
    }

    [Fact]
    public void OperationStarted_ShouldFireEvent()
    {
        var tracker = new VsOperationTracker();
        VsOperationCall? captured = null;
        tracker.OperationStarted += op => captured = op;

        using var scope = tracker.Start("test");

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("test");
        captured.State.Should().Be(VsOperationState.Running);
    }

    [Fact]
    public void OperationCompleted_ShouldFireEvent()
    {
        var tracker = new VsOperationTracker();
        VsOperationCall? captured = null;
        tracker.OperationCompleted += op => captured = op;

        using (var scope = tracker.Start("test"))
        {
            scope.SetResult("ok");
        }

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("test");
        captured.State.Should().Be(VsOperationState.Completed);
    }

    [Fact]
    public void OperationCompleted_ShouldFireOnError()
    {
        var tracker = new VsOperationTracker();
        VsOperationCall? captured = null;
        tracker.OperationCompleted += op => captured = op;

        using (var scope = tracker.Start("test"))
        {
            scope.SetError("fail");
        }

        captured.Should().NotBeNull();
        captured!.State.Should().Be(VsOperationState.Failed);
        captured.Error.Should().Be("fail");
    }

    [Fact]
    public void MultipleStartChild_ShouldTrackAllChildren()
    {
        var tracker = new VsOperationTracker();

        using (var parent = tracker.Start("parent"))
        {
            using (var c1 = parent.StartChild("child1")) { c1.SetResult("r1"); }
            using (var c2 = parent.StartChild("child2")) { c2.SetResult("r2"); }
            using (var c3 = parent.StartChild("child3")) { c3.SetResult("r3"); }
            parent.SetResult("done");
        }

        tracker.History[0].Children.Should().HaveCount(3);
        tracker.History[0].Children![0].Name.Should().Be("child1");
        tracker.History[0].Children![1].Name.Should().Be("child2");
        tracker.History[0].Children![2].Name.Should().Be("child3");
    }

    [Fact]
    public void ElapsedMs_ShouldReflectRealTime()
    {
        var tracker = new VsOperationTracker();

        using (var scope = tracker.Start("slow"))
        {
            Thread.Sleep(10);
            scope.SetResult("done");
        }

        tracker.History[0].ElapsedMs.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void SetResultAfterDispose_ShouldNotThrow()
    {
        var tracker = new VsOperationTracker();
        VsOperationScope scope;

        using (scope = tracker.Start("op"))
        {
            scope.SetResult("ok");
        }

        // Should not throw
        scope.SetResult("again");
        scope.SetError("late error");
    }

    [Fact]
    public async Task History_ShouldBeThreadSafe()
    {
        var tracker = new VsOperationTracker();
        var tasks = new List<Task>();

        for (var i = 0; i < 20; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                using var scope = tracker.Start($"thread-op-{index}");
                scope.SetResult("ok");
            }));
        }

        await Task.WhenAll(tasks);

        tracker.History.Should().HaveCount(20);
    }

    [Fact]
    public void StartChild_OnDisposedParent_ShouldCreateOrphan()
    {
        var tracker = new VsOperationTracker();
        VsOperationScope child;

        using (var parent = tracker.Start("parent"))
        {
            child = parent.StartChild("orphan");
            child.SetResult("orphan-result");
        }

        // Parent is completed, child should still be tracked
        tracker.History[0].Children.Should().NotBeNull();
        tracker.History[0].Children![0].State.Should().Be(VsOperationState.Completed);
        tracker.History[0].Children![0].Result.Should().Be("orphan-result");
    }

    [Fact]
    public void StartChild_OnDisposedScope_ShouldThrow()
    {
        var tracker = new VsOperationTracker();
        VsOperationScope parent;

        using (parent = tracker.Start("parent")) { }

        Action act = () => parent.StartChild("too-late");
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Children_ShouldNotFireOperationCompleted()
    {
        var tracker = new VsOperationTracker();
        var captured = new List<VsOperationCall>();
        tracker.OperationCompleted += op => captured.Add(op);

        using (var parent = tracker.Start("parent"))
        {
            using (var child = parent.StartChild("child"))
            {
                child.SetResult("ok");
            }
            parent.SetResult("done");
        }

        // Only parent should fire OperationCompleted, not children
        captured.Should().HaveCount(1);
        captured[0].Name.Should().Be("parent");
    }

    [Fact]
    public void Children_ShouldNotBeInHistory()
    {
        var tracker = new VsOperationTracker();

        using (var parent = tracker.Start("parent"))
        {
            using (var child = parent.StartChild("child"))
            {
                child.SetResult("ok");
            }
            parent.SetResult("done");
        }

        tracker.History.Should().HaveCount(1);
        tracker.History[0].Name.Should().Be("parent");
    }
}

[Trait("Category", "Unit")]
public sealed class VsOperationCallRecordTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var now = DateTime.UtcNow;
        var call = new VsOperationCall(
            Id: "id-1",
            Name: "test",
            Arguments: "args",
            State: VsOperationState.Running,
            Result: null,
            Error: null,
            ElapsedMs: 0,
            StartedAt: now,
            Children: null
        );

        call.Id.Should().Be("id-1");
        call.Name.Should().Be("test");
        call.Arguments.Should().Be("args");
        call.State.Should().Be(VsOperationState.Running);
        call.Result.Should().BeNull();
        call.Error.Should().BeNull();
        call.ElapsedMs.Should().Be(0);
        call.StartedAt.Should().Be(now);
        call.Children.Should().BeNull();
    }

    [Fact]
    public void With_StateChange_ShouldCreateNewRecord()
    {
        var call = new VsOperationCall(
            Id: "id-1", Name: "test", Arguments: null,
            State: VsOperationState.Running, Result: null, Error: null,
            ElapsedMs: 0, StartedAt: DateTime.UtcNow, Children: null
        );

        var completed = call with { State = VsOperationState.Completed, Result = "done", ElapsedMs = 100 };

        completed.State.Should().Be(VsOperationState.Completed);
        completed.Result.Should().Be("done");
        completed.ElapsedMs.Should().Be(100);
        // Original unchanged
        call.State.Should().Be(VsOperationState.Running);
    }
}

[Trait("Category", "Unit")]
public sealed class VsOperationStateTests
{
    [Fact]
    public void Enum_ShouldHaveFourValues()
    {
        var values = (VsOperationState[])Enum.GetValues(typeof(VsOperationState));
        values.Should().HaveCount(4);
        values.Should().Contain(VsOperationState.Running);
        values.Should().Contain(VsOperationState.Completed);
        values.Should().Contain(VsOperationState.Failed);
        values.Should().Contain(VsOperationState.Cancelled);
    }
}
