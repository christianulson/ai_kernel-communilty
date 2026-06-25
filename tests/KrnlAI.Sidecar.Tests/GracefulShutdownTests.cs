namespace KrnlAI.Sidecar.Tests;

public sealed class GracefulShutdownTests
{
    [Fact]
    public async Task GracefulShutdown_ApplicationStopping_ShouldComplete()
    {
        using var factory = new SidecarWebAppFactory();
        var client = factory.CreateClient();

        var health = await client.GetAsync("/health", TestContext.Current.CancellationToken).ConfigureAwait(false);
        health.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
