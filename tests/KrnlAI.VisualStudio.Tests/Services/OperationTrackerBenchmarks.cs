using System.Diagnostics;
using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace KrnlAI.VisualStudio.Tests.Services;

[Trait("Category", "Benchmark")]
public sealed class OperationTrackerBenchmarks
{
    private readonly ITestOutputHelper _output;
    private const int Iterations = 10_000;

    public OperationTrackerBenchmarks(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void OperationTracker_SingleThread_10kOps()
    {
        var tracker = new VsOperationTracker();
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < Iterations; i++)
        {
            using var op = tracker.Start($"op-{i}");
            op.SetResult("ok");
        }

        sw.Stop();
        var avg = sw.ElapsedMilliseconds / (double)Iterations;

        _output.WriteLine($"Total: {sw.ElapsedMilliseconds}ms for {Iterations} ops");
        _output.WriteLine($"Avg: {avg:F3}ms per op");
        _output.WriteLine($"Throughput: {Iterations * 1000 / Math.Max(1, sw.ElapsedMilliseconds)} ops/sec");

        tracker.History.Count.Should().Be(500); // MaxHistoryItems
        avg.Should().BeLessThan(0.5);
    }

    [Fact]
    public async Task OperationTracker_Concurrent_1kOps()
    {
        var tracker = new VsOperationTracker();
        var parallelOps = 1000;

        var sw = Stopwatch.StartNew();

        var tasks = new List<Task>();
        for (var i = 0; i < parallelOps; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                using var op = tracker.Start($"thread-{index}");
                op.SetResult("ok");
            }));
        }

        await Task.WhenAll(tasks);
        sw.Stop();
        var avg = sw.ElapsedMilliseconds / (double)parallelOps;

        _output.WriteLine($"Total: {sw.ElapsedMilliseconds}ms for {parallelOps} concurrent ops");
        _output.WriteLine($"Avg: {avg:F3}ms per op");
        _output.WriteLine($"Throughput: {parallelOps * 1000 / Math.Max(1, sw.ElapsedMilliseconds)} ops/sec");

        tracker.History.Count.Should().Be(500); // MaxHistoryItems
    }

    [Fact]
    public void OperationTracker_NestedChildren_1kOps()
    {
        var tracker = new VsOperationTracker();
        var sw = Stopwatch.StartNew();
        var totalOps = 0;

        for (var i = 0; i < 100; i++)
        {
            using var parent = tracker.Start($"parent-{i}");
            for (var j = 0; j < 10; j++)
            {
                using var child = parent.StartChild($"child-{i}.{j}");
                child.SetResult("ok");
                totalOps++;
            }
            parent.SetResult("done");
            totalOps++;
        }

        sw.Stop();
        var avg = sw.ElapsedMilliseconds / (double)totalOps;

        _output.WriteLine($"Total: {sw.ElapsedMilliseconds}ms for {totalOps} nested ops");
        _output.WriteLine($"Avg: {avg:F3}ms per op");

        tracker.History.Count.Should().Be(100);
        avg.Should().BeLessThan(0.5);
    }

    [Fact]
    public void OperationTracker_HistoryTrimming()
    {
        var tracker = new VsOperationTracker();

        for (var i = 0; i < 1000; i++)
        {
            using var op = tracker.Start($"op-{i}");
            op.SetResult("ok");
        }

        tracker.History.Count.Should().Be(500);
        tracker.History[0].Name.Should().Be("op-500");
    }

    [Fact]
    public async Task VsDebugService_DelegateOverhead()
    {
        var callCount = 0;
        var service = new VsDebugService(
            launchAction: () => callCount++,
            state: DebugState.Stopped);

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000; i++)
        {
            await service.LaunchProjectAsync();
            // Reset state via reflection for benchmark (internal state)
            var field = typeof(VsDebugService).GetField("_state",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(service, DebugState.Stopped);
        }

        sw.Stop();
        var avg = sw.ElapsedMilliseconds / 1000.0;

        _output.WriteLine($"Avg call: {avg:F3}ms for 1000 delegate invocations");
        callCount.Should().Be(1000);
        avg.Should().BeLessThan(1.0);
    }
}
