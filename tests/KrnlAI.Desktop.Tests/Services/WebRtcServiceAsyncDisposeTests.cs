using KrnlAI.Desktop.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class WebRtcServiceAsyncDisposeTests
{
    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var service = new WebRtcService(NullLogger<WebRtcService>.Instance);
        var ex = Record.Exception(() => service.Dispose());
        Assert.Null(ex);
    }
}
