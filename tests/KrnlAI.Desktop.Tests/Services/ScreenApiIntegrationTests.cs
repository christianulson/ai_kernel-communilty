using System.Text;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using Refit;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class ScreenApiIntegrationTests
{
    [Fact]
    public async Task UserServices_Load_ShouldReadActualApiResponse()
    {
        using var handler = new RecordingHandler("""[{"serviceType":"github","configured":true,"enabled":false,"lastUsedAt":null}]""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://desktop.test") };
        var vm = new UserServicesViewModel(new KernelClient(RestService.For<IGatewayApi>(http), new AuthTokenProvider()));
        await vm.LoadAsync();
        Assert.Equal("/user/services", handler.Path);
        Assert.Equal("github", Assert.Single(vm.Services).ServiceType);
        Assert.False(vm.Services[0].Enabled);
    }

    [Fact]
    public async Task UserServices_Update_ShouldSendCredentialsAndEnabledToApi()
    {
        using var handler = new RecordingHandler("{\"ok\":true}");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://desktop.test") };
        var client = new KernelClient(RestService.For<IGatewayApi>(http), new AuthTokenProvider());
        Assert.True(await client.UpdateUserServiceAsync("github", new UserServiceUpdateRequest(new() { ["token"] = "test-token" }, false)));
        Assert.Equal(HttpMethod.Put, handler.Method);
        Assert.Equal("/user/services/github", handler.Path);
        Assert.Contains("test-token", handler.Body);
        Assert.Contains("\"enabled\":false", handler.Body);
    }

    [Fact]
    public async Task KernelClient_FailedRequest_ShouldPropagateErrorToScreen()
    {
        using var handler = new RecordingHandler("{\"error\":\"offline\"}", HttpStatusCode.ServiceUnavailable);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://desktop.test") };
        var client = new KernelClient(RestService.For<IGatewayApi>(http), new AuthTokenProvider());
        await Assert.ThrowsAsync<ApiException>(() => client.GetPoliciesAsync(null, 1, 100));
    }

    [Fact]
    public async Task KernelClient_HealthUnavailable_ShouldReturnFalse()
    {
        using var handler = new RecordingHandler("{}", HttpStatusCode.ServiceUnavailable);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://desktop.test") };
        var client = new KernelClient(RestService.For<IGatewayApi>(http), new AuthTokenProvider());
        Assert.False(await client.CheckHealthAsync());
    }

    private sealed class RecordingHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Path { get; private set; }
        public string Body { get; private set; } = "";
        public HttpMethod? Method { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            Method = request.Method;
            Body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json"), RequestMessage = request };
        }
    }
}
