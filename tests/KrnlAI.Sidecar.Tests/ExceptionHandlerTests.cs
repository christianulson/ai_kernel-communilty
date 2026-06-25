namespace KrnlAI.Sidecar.Tests;

public sealed class ExceptionHandlerTests(SidecarWebAppFactory factory) : IClassFixture<SidecarWebAppFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task ExceptionHandler_NonexistentEndpoint_ShouldReturn404()
    {
        var res = await _http.GetAsync("/nonexistent", TestContext.Current.CancellationToken).ConfigureAwait(false);

        res.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_ShouldNotExposeStackTrace()
    {
        var res = await _http.GetAsync("/health", TestContext.Current.CancellationToken).ConfigureAwait(false);

        var body = await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        body.Should().NotContain("StackTrace");
        body.Should().NotContain("Exception");
    }
}
