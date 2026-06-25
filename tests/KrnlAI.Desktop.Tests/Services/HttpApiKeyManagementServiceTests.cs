using System.Net.Http.Json;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class HttpApiKeyManagementServiceTests
{
    [Fact]
    public async Task ListAsync_ShouldMapApiKeys()
    {
        var handler = new ScriptedHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/account/api-keys", req.RequestUri!.AbsolutePath);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new
                    {
                        keyId = "kid-1",
                        keyPrefix = "krnl_abcd1234",
                        name = "ci",
                        scope = ApiKeyScope.ReadWrite,
                        createdAt = "2026-05-27T10:00:00+00:00",
                        expiresAt = "2026-06-27T10:00:00+00:00",
                        lastUsedAt = "2026-05-28T10:00:00+00:00",
                        active = true
                    }
                })
            };
        });

        var service = new HttpApiKeyManagementService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var items = await service.ListAsync(CancellationToken.None);

        Assert.Single(items);
        Assert.Equal("kid-1", items[0].KeyId);
        Assert.Equal(ApiKeyScope.ReadWrite, items[0].Scope);
        Assert.True(items[0].Active);
        Assert.Contains("••••", items[0].DisplayPrefix);
    }

    [Fact]
    public async Task CreateAsync_ShouldPostPayloadAndReturnKey()
    {
        var handler = new ScriptedHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/account/api-keys", req.RequestUri!.AbsolutePath);

            var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("\"name\":\"ci-pipeline\"", body);
            Assert.Contains("\"scope\":1", body);
            Assert.Contains("\"ttl\":\"30.00:00:00\"", body);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    keyId = "kid-2",
                    fullKey = "krnl_full_secret",
                    name = "ci-pipeline",
                    scope = ApiKeyScope.ReadWrite,
                    expiresAt = "2026-06-27T10:00:00+00:00",
                    warning = "Copie esta chave agora."
                })
            };
        });

        var service = new HttpApiKeyManagementService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var created = await service.CreateAsync(new ApiKeyCreationRequest("ci-pipeline", TimeSpan.FromDays(30), ApiKeyScope.ReadWrite), CancellationToken.None);

        Assert.Equal("kid-2", created.KeyId);
        Assert.Equal("krnl_full_secret", created.FullKey);
        Assert.Equal(ApiKeyScope.ReadWrite, created.Scope);
    }

    [Fact]
    public Task RevokeAsync_ShouldCallRevokeEndpoint()
    {
        var handler = new ScriptedHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("/account/api-keys/kid-3/revoke", req.RequestUri!.AbsolutePath);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { revoked = true })
            };
        });

        var service = new HttpApiKeyManagementService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        return service.RevokeAsync("kid-3", CancellationToken.None);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldMapSummary()
    {
        var handler = new ScriptedHandler(req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("/account/api-keys/stats", req.RequestUri!.AbsolutePath);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    total = 4,
                    active = 2,
                    expired = 1,
                    revoked = 1,
                    lastUsed = "2026-05-28T10:00:00+00:00"
                })
            };
        });

        var service = new HttpApiKeyManagementService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var stats = await service.GetStatsAsync(CancellationToken.None);

        Assert.Equal(4, stats.Total);
        Assert.Equal(2, stats.Active);
        Assert.Equal(1, stats.Expired);
        Assert.Equal(1, stats.Revoked);
        Assert.Equal(new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero), stats.LastUsedAt);
    }

    private sealed class ScriptedHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(send(request));
    }
}
