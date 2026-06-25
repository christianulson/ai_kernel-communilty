using AutoFixture;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Moq;
using TestHelpers;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ApiKeysViewModelTests
{
    private static readonly IFixture Fixture = AutoMoq.CreateFixture();

    [Fact]
    public async Task LoadAsync_ShouldPopulateKeysAndSummary()
    {
        var service = new Mock<IApiKeyManagementService>();
        service.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new("kid-1", "krnl_abcd1234", "ci", ApiKeyScope.ReadWrite,
                    new DateTimeOffset(2026, 5, 27, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero),
                    true)
            ]);
        service.Setup(x => x.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyUsageSummary(1, 1, 0, 0, new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero)));

        var vm = new ApiKeysViewModel(service.Object);

        await vm.LoadAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.Single(vm.Keys);
        Assert.Contains("1 chave ativa", vm.StatusMessage);
        Assert.Equal("ci", vm.Keys[0].Name);
        Assert.Contains("••••", vm.Keys[0].DisplayPrefix);
    }

    [Fact]
    public async Task CreateAsync_ShouldExposeFullKeyOnceAndRefresh()
    {
        var service = new Mock<IApiKeyManagementService>();
        service.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        service.Setup(x => x.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyUsageSummary(0, 0, 0, 0, null));
        service.Setup(x => x.CreateAsync(It.IsAny<ApiKeyCreationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKeyCreationResult("kid-2", "krnl_full_secret", "pipeline",
                ApiKeyScope.ReadWrite, new DateTimeOffset(2026, 6, 27, 10, 0, 0, TimeSpan.Zero),
                "Copie esta chave agora."));

        var vm = new ApiKeysViewModel(service.Object)
        {
            NameInput = "pipeline",
            TtlDaysInput = 30,
            SelectedScope = ApiKeyScope.ReadWrite
        };

        await vm.CreateAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.Equal("krnl_full_secret", vm.CreatedFullKey);
        Assert.Equal("pipeline", vm.CreatedName);
        Assert.Equal("Chave criada. Copie o valor agora.", vm.StatusMessage);
    }
}
