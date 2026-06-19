using System.CommandLine;
using KrnlAI.Cli.Commands;
using KrnlAI.Core.Abstractions;
using Spectre.Console.Testing;

namespace KrnlAI.Cli.Tests;

public sealed class PluginCommandTests
{
    [Fact]
    public void PluginCommand_Build_ShouldCreateCommand()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();

        cmd.Name.Should().Be("plugin");
        cmd.Children.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task PluginCommand_Install_ShouldShowInstallMessage()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin install /tmp/test.dll --endpoint http://localhost:9999").InvokeAsync();

        console.Output.Should().Contain("Installing plugin");
    }

    [Fact]
    public async Task PluginCommand_Remove_Local_ShouldSucceed()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin remove test-plugin --local").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("removed");
    }

    [Fact]
    public async Task PluginCommand_MapType_ShouldInstallLocalSuccessfully()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin install test.dll --type dotnet-assembly --local").InvokeAsync();

        result.Should().Be(0);
    }

    [Fact]
    public async Task PluginCommand_Info_ShouldShowPluginDetails()
    {
        var console = new TestConsole();
        var fakeCatalog = new FakePluginCatalog(
            new PluginCatalogEntry("test-plugin", "Test Plugin", "A test plugin", "author",
                "1.0", ["test"], 100, true, DateTimeOffset.UtcNow));
        var cmd = new PluginCommand(console, catalog: fakeCatalog).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin info test-plugin").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Test Plugin");
        console.Output.Should().Contain("1.0");
        console.Output.Should().Contain("author");
    }

    [Fact]
    public async Task PluginCommand_Info_NotFound_ShouldShowMessage()
    {
        var console = new TestConsole();
        var fakeCatalog = new FakePluginCatalog();
        var cmd = new PluginCommand(console, catalog: fakeCatalog).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin info nonexistent").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("not found");
    }

    [Fact]
    public async Task PluginCommand_Info_NoCatalog_ShouldShowError()
    {
        var console = new TestConsole();
        var cmd = new PluginCommand(console).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin info test").InvokeAsync();

        result.Should().Be(1);
        console.Output.Should().Contain("not available");
    }

    [Fact]
    public async Task PluginCommand_Search_ShouldReturnResults()
    {
        var console = new TestConsole();
        var fakeCatalog = new FakePluginCatalog(
            new PluginCatalogEntry("result-1", "Result One", "First result", "author",
                "1.0", ["test"], 10, true, DateTimeOffset.UtcNow),
            new PluginCatalogEntry("result-2", "Result Two", "Second result", "author",
                "2.0", ["demo"], 20, false, DateTimeOffset.UtcNow));
        var cmd = new PluginCommand(console, catalog: fakeCatalog).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin search result").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("Result One");
        console.Output.Should().Contain("Result Two");
    }

    [Fact]
    public async Task PluginCommand_Search_NoMatch_ShouldShowEmpty()
    {
        var console = new TestConsole();
        var fakeCatalog = new FakePluginCatalog();
        var cmd = new PluginCommand(console, catalog: fakeCatalog).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin search nonexistent").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("No plugins found");
    }

    [Fact]
    public async Task PluginCommand_RegistryAdd_ShouldPersist()
    {
        var console = new TestConsole();
        var fakeRegistry = new FakePluginRegistryService();
        var cmd = new PluginCommand(console, registry: fakeRegistry).Build();
        var root = new RootCommand { cmd };

        var result = await root.Parse("plugin registry add my-reg http://my.registry").InvokeAsync();

        result.Should().Be(0);
        console.Output.Should().Contain("added");

        var registries = await fakeRegistry.ListRegistriesAsync();
        registries.Should().Contain(r => r.Id == "my-reg" && r.Url == "http://my.registry");
    }

    private sealed class FakePluginCatalog(params PluginCatalogEntry[] entries) : IPluginCatalog
    {
        private readonly List<PluginCatalogEntry> _entries = [.. entries];

        public Task<PluginSearchResult> SearchAsync(string query, CancellationToken ct = default)
        {
            var q = query.ToLowerInvariant();
            var matches = _entries.Where(e =>
                e.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult(new PluginSearchResult(matches, matches.Count));
        }

        public Task<PluginCatalogEntry?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_entries.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<PluginCatalogEntry>> ListByTagAsync(string tag, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginCatalogEntry>>([.. _entries.Where(e => e.Tags.Contains(tag))]);

        public Task<PluginCatalogEntry?> GetLatestVersionAsync(string id, CancellationToken ct = default)
            => GetByIdAsync(id, ct);
    }

    private sealed class FakePluginRegistryService : IPluginRegistryService
    {
        private readonly List<PluginRegistryConfig> _registries = [];

        public Task AddRegistryAsync(PluginRegistryConfig config, CancellationToken ct = default)
        {
            _registries.Add(config);
            return Task.CompletedTask;
        }

        public Task RemoveRegistryAsync(string registryId, CancellationToken ct = default)
        {
            _registries.RemoveAll(r => r.Id == registryId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PluginRegistryConfig>> ListRegistriesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginRegistryConfig>>([.. _registries]);

        public Task SyncAllAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<IReadOnlyList<PluginCatalogEntry>> GetMergedCatalogAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PluginCatalogEntry>>([]);
    }
}
