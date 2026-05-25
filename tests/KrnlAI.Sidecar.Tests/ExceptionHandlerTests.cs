namespace KrnlAI.Sidecar.Tests;

public sealed class ExceptionHandlerTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task ExceptionHandler_NonexistentEndpoint_ShouldReturn404()
    {
        var res = await _http.GetAsync("/nonexistent", TestContext.Current.CancellationToken);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_ShouldNotExposeStackTrace()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken);

        var body = await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().NotContain("StackTrace");
        body.Should().NotContain("Exception");
    }
}
