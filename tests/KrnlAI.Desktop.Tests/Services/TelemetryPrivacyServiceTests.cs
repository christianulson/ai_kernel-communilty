using System.Net.Http.Json;
using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class TelemetryPrivacyServiceTests
{
    [Fact]
    public async Task GetConsentAsync_ShouldMapResponse()
    {
        var handler = new ScriptedHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/api/privacy/telemetry/consent", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    consentLevel = "Anonymous",
                    grantedAt = "2026-05-27T10:00:00+00:00",
                    revokedAt = (string?)null
                })
            };
        });

        var service = new HttpTelemetryPrivacyService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var state = await service.GetConsentAsync(CancellationToken.None);

        Assert.Equal(TelemetryConsentLevel.Anonymous, state.ConsentLevel);
        Assert.Equal(new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero), state.GrantedAt);
        Assert.Null(state.RevokedAt);
    }

    [Fact]
    public async Task SetConsentAsync_ShouldPostConsentLevel()
    {
        var handler = new ScriptedHandler(req =>
        {
            Assert.Equal(HttpMethod.Put, req.Method);
            Assert.Equal("/api/privacy/telemetry/consent", req.RequestUri!.AbsolutePath);

            var json = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("\"consentLevel\":\"Full\"", json);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    consentLevel = "Full",
                    grantedAt = "2026-05-27T10:30:00+00:00",
                    revokedAt = (string?)null
                })
            };
        });

        var service = new HttpTelemetryPrivacyService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var state = await service.SetConsentAsync(TelemetryConsentLevel.Full, CancellationToken.None);

        Assert.Equal(TelemetryConsentLevel.Full, state.ConsentLevel);
        Assert.Equal(new DateTimeOffset(2026, 5, 27, 10, 30, 0, TimeSpan.Zero), state.GrantedAt);
    }

    [Fact]
    public async Task RequestExportAsync_ShouldReturnRequestId()
    {
        var handler = new ScriptedHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/api/privacy/telemetry/export", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = JsonContent.Create(new
                {
                    requestId = "req-123",
                    status = "pending",
                    message = "accepted"
                })
            };
        });

        var service = new HttpTelemetryPrivacyService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await service.RequestExportAsync(CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Equal("req-123", result.RequestId);
        Assert.Equal("accepted", result.Message);
    }

    [Fact]
    public async Task RequestDeletionAsync_ShouldReturnRequestId()
    {
        var handler = new ScriptedHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/api/privacy/telemetry/delete", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = JsonContent.Create(new
                {
                    requestId = "req-456",
                    status = "pending",
                    message = "accepted"
                })
            };
        });

        var service = new HttpTelemetryPrivacyService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await service.RequestDeletionAsync(CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.Equal("req-456", result.RequestId);
        Assert.Equal("accepted", result.Message);
    }

    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(send(request));
    }
}
