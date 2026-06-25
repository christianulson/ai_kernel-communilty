using Microsoft.AspNetCore.Mvc.Testing;

namespace KrnlAI.Sidecar.Tests;

public sealed class BodySizeLimitTests(SmallBodyWebAppFactory factory) : IClassFixture<SmallBodyWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task BodySize_UnderLimit_ShouldSucceed()
    {
        var payload = new { prompt = new string('x', 500) };
        var res = await _http.PostAsJsonAsync("/agent/run", payload, TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

}

public sealed class SmallBodyWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("https_port", "5001");
    }
}
