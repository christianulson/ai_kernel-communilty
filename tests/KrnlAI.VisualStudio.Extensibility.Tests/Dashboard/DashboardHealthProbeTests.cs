using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Dashboard;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Dashboard;

public sealed class DashboardHealthProbeTests
{
    private sealed class FakeKernel : IKernelClientService
    {
        public ConnectionState State { get; set; }
        public event Action<ConnectionState>? StateChanged { add { } remove { } }
        public string? BaseUrl => null;
        public bool Healthy { get; set; }
        public bool ThrowOnHealth { get; set; }
        public Task<bool> ConnectAsync(string endpoint, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> CheckHealthAsync(CancellationToken ct = default)
            => ThrowOnHealth
                ? throw new HttpRequestException("kernel unreachable")
                : Task.FromResult(Healthy);
        public Task<KrnlAI.Sdk.Models.AgentRunResponse> RunAgentAsync(
            string goal, KrnlAI.Sdk.Models.AgentRunRequest? request = null, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    [Fact]
    public async Task ProbeAsync_HealthyKernel_ShouldMarkConnected()
    {
        var kernel = new FakeKernel { Healthy = true };
        var model = new DashboardModel();

        await DashboardHealthProbe.ProbeAsync(kernel, model, CancellationToken.None);

        model.KernelStatus.Should().Be(DashboardModel.ConnectionStatus.Connected);
    }

    [Fact]
    public async Task ProbeAsync_UnhealthyKernel_ShouldMarkDisconnected()
    {
        var kernel = new FakeKernel { Healthy = false };
        var model = new DashboardModel();

        await DashboardHealthProbe.ProbeAsync(kernel, model, CancellationToken.None);

        model.KernelStatus.Should().Be(DashboardModel.ConnectionStatus.Disconnected);
    }

    [Fact]
    public async Task ProbeAsync_KernelThrows_ShouldMarkDisconnected()
    {
        var kernel = new FakeKernel { ThrowOnHealth = true };
        var model = new DashboardModel();

        await DashboardHealthProbe.ProbeAsync(kernel, model, CancellationToken.None);

        model.KernelStatus.Should().Be(DashboardModel.ConnectionStatus.Disconnected);
    }
}