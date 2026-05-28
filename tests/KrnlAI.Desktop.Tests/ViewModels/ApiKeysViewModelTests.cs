using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ApiKeysViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldPopulateKeysAndSummary()
    {
        var service = new FakeApiKeyManagementService
        {
            Keys = new[]
            {
                new ApiKeyListItem(
                    "kid-1",
                    "krnl_abcd1234",
                    "ci",
                    ApiKeyScope.ReadWrite,
                    new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero),
                    true)
            },
            Stats = new ApiKeyUsageSummary(1, 1, 0, 0, new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero))
        };

        var vm = new ApiKeysViewModel(service);

        await vm.LoadAsync(CancellationToken.None);

        Assert.Single(vm.Keys);
        Assert.Contains("1 chave ativa", vm.StatusMessage);
        Assert.Equal("ci", vm.Keys[0].Name);
        Assert.Contains("••••", vm.Keys[0].DisplayPrefix);
    }

    [Fact]
    public async Task CreateAsync_ShouldExposeFullKeyOnceAndRefresh()
    {
        var service = new FakeApiKeyManagementService
        {
            Keys = Array.Empty<ApiKeyListItem>(),
            Created = new ApiKeyCreationResult(
                "kid-2",
                "krnl_full_secret",
                "pipeline",
                ApiKeyScope.ReadWrite,
                new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero),
                "Copie esta chave agora.")
        };

        var vm = new ApiKeysViewModel(service)
        {
            NameInput = "pipeline",
            TtlDaysInput = 30,
            SelectedScope = ApiKeyScope.ReadWrite
        };

        await vm.CreateAsync(CancellationToken.None);

        Assert.Equal("krnl_full_secret", vm.CreatedFullKey);
        Assert.Equal("pipeline", vm.CreatedName);
        Assert.Equal("Chave criada. Copie o valor agora.", vm.StatusMessage);
    }

    private sealed class FakeApiKeyManagementService : IApiKeyManagementService
    {
        public IReadOnlyList<ApiKeyListItem> Keys { get; init; } = Array.Empty<ApiKeyListItem>();
        public ApiKeyUsageSummary? Stats { get; init; }
        public ApiKeyCreationResult? Created { get; init; }

        public Task<IReadOnlyList<ApiKeyListItem>> ListAsync(CancellationToken ct = default)
            => Task.FromResult(Keys);

        public Task<ApiKeyCreationResult> CreateAsync(ApiKeyCreationRequest request, CancellationToken ct = default)
            => Task.FromResult(Created ?? new ApiKeyCreationResult("kid", "krnl_test", request.Name, request.Scope, DateTimeOffset.UtcNow, "ok"));

        public Task RevokeAsync(string keyId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<ApiKeyUsageSummary> GetStatsAsync(CancellationToken ct = default)
            => Task.FromResult(Stats ?? new ApiKeyUsageSummary(0, 0, 0, 0, null));
    }
}
