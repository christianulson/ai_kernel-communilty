using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Infrastructure.Abstractions;
using KrnlAI.Desktop.Infrastructure.KernelClient;
using Moq;

namespace KrnlAI.Desktop.Tests.Services;

public class KernelClientTests
{
    private (KernelClient Client, Mock<IGatewayApi> ApiMock, AuthTokenProvider TokenProvider) CreateClient()
    {
        var apiMock = new Mock<IGatewayApi>(MockBehavior.Strict);
        var tokenProvider = new AuthTokenProvider();
        var client = new KernelClient(apiMock.Object, tokenProvider);
        return (client, apiMock, tokenProvider);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOk_ReturnsTrue()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthResponse(true, DateTimeOffset.UtcNow));

        var result = await client.CheckHealthAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNotOk_ReturnsFalse()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthResponse(false, DateTimeOffset.UtcNow));

        var result = await client.CheckHealthAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenFails_ReturnsFalse()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.GetHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var result = await client.CheckHealthAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task RunAgentAsync_ShouldReturnResponse()
    {
        var (client, apiMock, _) = CreateClient();
        var dto = new AgentRunResponseDto("Hello!", null, null, null, null);
        apiMock.Setup(a => a.RunAgentAsync(It.IsAny<AgentRunRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await client.RunAgentAsync(new AgentRunRequest("Hi"));
        Assert.Equal("Hello!", result.Narration);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task RunAgentAsync_WhenApiThrows_ReturnsNullNarration()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.RunAgentAsync(It.IsAny<AgentRunRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API down"));

        var result = await client.RunAgentAsync(new AgentRunRequest("Hi"));
        Assert.Null(result.Narration);
        Assert.Null(result.Error); // Error is not preserved; SafeCall logs it instead
    }

    [Fact]
    public async Task LoginAsync_WhenValid_ShouldReturnToken()
    {
        var (client, apiMock, _) = CreateClient();
        var userInfo = new LoginUserInfoDto("admin-001", "admin@ai-kernel.local", "Admin", ["admin"]);
        apiMock.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponseDto("jwt-token", "refresh-abc", userInfo));

        var result = await client.LoginAsync(new LoginRequest("admin@ai-kernel.local", "pass"));
        Assert.True(result.Success);
        Assert.Equal("jwt-token", result.Token);
        Assert.Equal("admin@ai-kernel.local", result.Username);
        Assert.Equal("refresh-abc", result.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_WhenInvalid_ShouldReturnFailure()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("401"));

        var result = await client.LoginAsync(new LoginRequest("bad@email.com", "wrong"));
        Assert.False(result.Success);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task GetPoliciesAsync_WhenAvailable_ShouldReturnList()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.GetPoliciesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyListResponseDto(
                [new PolicyInfoDto("p1", "Policy 1", "http", "1.0", DateTime.UtcNow, null, true)], 1, 1, 20));

        var result = await client.GetPoliciesAsync();
        Assert.Single(result.Policies);
        Assert.Equal("Policy 1", result.Policies[0].Name);
    }

    [Fact]
    public async Task GetPoliciesAsync_WhenError_ShouldReturnEmpty()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.GetPoliciesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException());

        var result = await client.GetPoliciesAsync();
        Assert.Empty(result.Policies);
    }

    [Fact]
    public async Task GetActiveGoalsAsync_WhenAvailable_ShouldReturnList()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.GetActiveGoalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalListDto(
                [new GoalInfoDto("g1", "Test Goal", "active", 3, DateTime.UtcNow, null, null, null, 0, 0)], 1));

        var result = await client.GetActiveGoalsAsync();
        Assert.Single(result.Goals);
        Assert.Equal("Test Goal", result.Goals[0].Description);
    }

    [Fact]
    public async Task GetActiveGoalsAsync_WhenError_ShouldReturnEmpty()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.GetActiveGoalsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException());

        var result = await client.GetActiveGoalsAsync();
        Assert.Empty(result.Goals);
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldReturnGoal()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.CreateGoalAsync(It.IsAny<CreateGoalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GoalInfoDto("g1", "New Goal", "active", 2, DateTime.UtcNow, null, null, null, 0, 0));

        var result = await client.CreateGoalAsync(new CreateGoalRequest("New Goal", 2));
        Assert.NotNull(result);
        Assert.Equal("New Goal", result!.Description);
    }

    [Fact]
    public async Task UpdateGoalStatusAsync_WhenSuccess_ShouldReturnTrue()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.UpdateGoalStatusAsync("g1", "pause", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await client.UpdateGoalStatusAsync("g1", "pause");
        Assert.True(result);
    }

    [Fact]
    public async Task SearchMemoryAsync_WhenAvailable_ShouldReturnHits()
    {
        var (client, apiMock, _) = CreateClient();
        var dto = new MemorySearchResultDto(
            [new MemoryHitDto("h1", "test", "web", 0.9, DateTime.UtcNow, null)], 1, 0.5);
        apiMock.Setup(a => a.SearchMemoryAsync("test", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await client.SearchMemoryAsync("test");
        Assert.Single(result.Hits);
        Assert.Equal(0.9, result.Hits[0].Score);
    }

    [Fact]
    public async Task GetScorecardAsync_ShouldReturnScores()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.GetScorecardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScorecardDto(0.95, 0.88, 0.99, 0.92, 0.85, 0.92));

        var result = await client.GetScorecardAsync();
        Assert.NotNull(result);
        Assert.Equal(0.95, result!.Reliability);
        Assert.Equal(0.92, result.Overall);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ShouldReturnResult()
    {
        var (client, apiMock, _) = CreateClient();
        apiMock.Setup(a => a.SubmitFeedbackAsync(It.IsAny<FeedbackRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackResultDto(true, "fb1", "ok"));

        var result = await client.SubmitFeedbackAsync(new FeedbackRequest("ep1", 5, "Good", null));
        Assert.True(result.Success);
        Assert.Equal("fb1", result.FeedbackId);
    }

    [Fact]
    public void SetAuthToken_ShouldStoreInProvider()
    {
        var (client, _, tokenProvider) = CreateClient();
        Assert.Null(tokenProvider.Token);

        client.SetAuthToken("bearer-token");
        Assert.Equal("bearer-token", tokenProvider.Token);

        client.SetAuthToken(null);
        Assert.Null(tokenProvider.Token);
    }

    [Fact]
    public void SetTokens_ShouldStoreBothInProvider()
    {
        var (client, _, tokenProvider) = CreateClient();
        Assert.Null(tokenProvider.Token);
        Assert.Null(tokenProvider.RefreshToken);

        client.SetTokens("access", "refresh");

        Assert.Equal("access", tokenProvider.Token);
        Assert.Equal("refresh", tokenProvider.RefreshToken);
    }
}
