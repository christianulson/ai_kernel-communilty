using FluentAssertions;
using KrnlAI.Sdk.Models;
using KrnlAI.VisualStudio.Extensibility.Core.Chat;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Chat;

public sealed class KernelMessageSinkTests
{
    private sealed class FakeKernel : IKernelClientService
    {
        public ConnectionState State { get; set; } = ConnectionState.Connected;
        public event Action<ConnectionState>? StateChanged { add { } remove { } }
        public string? BaseUrl => "http://localhost:5235";
        public List<string> Goals { get; } = new();
        public AgentRunResponse? NextResponse { get; set; }
        public bool ThrowOnRun { get; set; }

        public Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default)
        {
            State = ConnectionState.Connected;
            return Task.FromResult(true);
        }

        public Task<bool> CheckHealthAsync(CancellationToken ct = default)
            => Task.FromResult(State == ConnectionState.Connected);

        public Task<AgentRunResponse> RunAgentAsync(
            string goal, AgentRunRequest? request = null, CancellationToken ct = default)
        {
            Goals.Add(goal);
            if (ThrowOnRun)
                throw new InvalidOperationException("kernel boom");
            return Task.FromResult(NextResponse ?? new AgentRunResponse(
                Goal: goal, Status: "completed", Summary: "done", Steps: Array.Empty<PlanStepResult>()));
        }
    }

    [Fact]
    public async Task SendAsync_Connected_ShouldRunAgentAndRaiseAssistantMessage()
    {
        var kernel = new FakeKernel();
        var sink = new KernelMessageSink(kernel);
        Core.Chat.ChatMessage? received = null;
        sink.AssistantMessageReceived += m => received = m;

        await sink.SendAsync("hello", CancellationToken.None);

        kernel.Goals.Should().ContainSingle().Which.Should().Be("hello");
        received.Should().NotBeNull();
        received!.Author.Should().Be("assistant");
        received.Text.Should().Be("done");
    }

    [Fact]
    public async Task SendAsync_Disconnected_ShouldNotRunAgentOrRaise()
    {
        var kernel = new FakeKernel { State = ConnectionState.Disconnected };
        var sink = new KernelMessageSink(kernel);
        Core.Chat.ChatMessage? received = null;
        sink.AssistantMessageReceived += m => received = m;

        await sink.SendAsync("hello", CancellationToken.None);

        kernel.Goals.Should().BeEmpty();
        received.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_KernelThrows_ShouldNotPropagate()
    {
        var kernel = new FakeKernel { ThrowOnRun = true };
        var sink = new KernelMessageSink(kernel);

        var act = async () => await sink.SendAsync("hello", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}