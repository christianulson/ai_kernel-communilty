using System.Text;
using AutoFixture;
using KrnlAI.Desktop.App.Services;
using Moq;
using Moq.Protected;
using TestHelpers;

namespace KrnlAI.Desktop.Tests.Services;

public sealed class PeerRankingManagementServiceTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public async Task HttpPeerRankingManagementService_GetRankingAsync_ShouldMapTierFromScore()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"ranking":[{"nodeId":"peer-1","overallScore":82,"successRateScore":90,"latencyScore":80,"availabilityScore":70,"tenureScore":60,"capacityScore":50,"catalogScore":40,"totalJobsExecuted":10,"totalJobsFailed":1,"avgResponseTimeMs":123,"uptimePercentage":95,"firstSeen":"2026-05-20T00:00:00Z","lastSeen":"2026-05-29T00:00:00Z","quarantineCount":0}],"enabled":true,"count":1}
                    """, Encoding.UTF8, "application/json")
            });

        var service = new HttpPeerRankingManagementService(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        });

        var ranking = await service.GetRankingAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.Single(ranking);
        Assert.Equal("peer-1", ranking[0].NodeId);
        Assert.Equal("Trusted", ranking[0].Tier);
        Assert.Equal(82, ranking[0].OverallScore);
    }

    [Fact]
    public async Task NullPeerRankingManagementService_ShouldReturnDefaults()
    {
        var service = new NullPeerRankingManagementService();

        var ranking = await service.GetRankingAsync(CancellationToken.None).ConfigureAwait(false);
        var weights = await service.GetWeightsAsync(CancellationToken.None).ConfigureAwait(false);
        var strategy = await service.GetStrategyAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.Empty(ranking);
        Assert.Equal(0.35, weights.SuccessRateWeight, 2);
        Assert.Equal("TopRanked", strategy.CurrentStrategyName);
    }

    [Fact]
    public async Task HttpPeerRankingManagementService_GetHistoryAsync_ShouldMapScoreSnapshots()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.AbsolutePath == "/p2p/ranking/history/peer-1"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [{"nodeId":"peer-1","eventType":"bonus","overallScore":88.5,"tier":"Trusted","delta":0.5,"reason":"job_success","timestamp":"2026-05-29T10:00:00Z"}]
                    """, Encoding.UTF8, "application/json")
            });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri == null || r.RequestUri.AbsolutePath != "/p2p/ranking/history/peer-1"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var service = new HttpPeerRankingManagementService(new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        });

        var history = await service.GetHistoryAsync("peer-1", CancellationToken.None).ConfigureAwait(false);

        Assert.Single(history);
        Assert.Equal("bonus", history[0].EventType);
        Assert.Equal(88.5, history[0].OverallScore, 2);
        Assert.Equal("Trusted", history[0].Tier);
    }
}
