using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Dashboard;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Dashboard;

public sealed class DashboardModelTests
{
    [Fact]
    public void Default_ShouldReportUnknownStatus()
    {
        var model = new DashboardModel();

        model.Version.Should().Be("1.0.0");
        model.KernelStatus.Should().Be(DashboardModel.ConnectionStatus.Unknown);
    }

    [Fact]
    public void UpdateStatus_ShouldChangeKernelStatus()
    {
        var model = new DashboardModel();

        model.UpdateKernelStatus(DashboardModel.ConnectionStatus.Connected);

        model.KernelStatus.Should().Be(DashboardModel.ConnectionStatus.Connected);
    }
}