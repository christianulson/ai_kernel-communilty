using System.Reflection;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using Refit;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class GatewayApiContractTests
{
    [Fact]
    public void SearchMemoryAsync_ShouldUseSidecarPostContract()
    {
        var method = typeof(IGatewayApi).GetMethod(nameof(IGatewayApi.SearchMemoryAsync));

        Assert.NotNull(method);
        Assert.Equal("/memory/search", method!.GetCustomAttribute<PostAttribute>()?.Path);
        var requestParameter = method.GetParameters().Single(p => p.Name == "request");
        Assert.NotNull(requestParameter.GetCustomAttribute<BodyAttribute>());
    }

    [Fact]
    public void GetObjectivesAsync_ShouldUseActiveObjectivesContract()
    {
        var method = typeof(IGatewayApi).GetMethod(nameof(IGatewayApi.GetObjectivesAsync));

        Assert.NotNull(method);
        Assert.Equal("/objectives/active", method!.GetCustomAttribute<GetAttribute>()?.Path);
    }
}
