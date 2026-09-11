using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Services.Settings;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Services;

public sealed class JsonSettingsStoreTests
{
    [Fact]
    public void RoundTrip_ShouldPersistValuesAcrossInstances()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"krnlai-settings-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "settings.json");

            var store = new JsonSettingsStore(path);
            store.SetString("KrnlAI", "Endpoint", "http://localhost:6000");
            store.SetInt32("KrnlAI", "SidecarPort", 5111);

            var reloaded = new JsonSettingsStore(path);
            reloaded.GetString("KrnlAI", "Endpoint", "").Should().Be("http://localhost:6000");
            reloaded.GetInt32("KrnlAI", "SidecarPort", 0).Should().Be(5111);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Get_MissingCollectionOrKey_ShouldReturnDefaults()
    {
        var store = new JsonSettingsStore(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json"));

        store.GetString("Nope", "Key", "dflt").Should().Be("dflt");
        store.GetInt32("Nope", "Key", 42).Should().Be(42);
        store.CollectionExists("Nope").Should().BeFalse();
    }

    [Fact]
    public void DeleteCollection_ShouldRemoveEntries()
    {
        var store = new JsonSettingsStore(Path.Combine(Path.GetTempPath(), $"del-{Guid.NewGuid():N}.json"));
        store.SetString("A", "K", "v");

        store.DeleteCollection("A");

        store.CollectionExists("A").Should().BeFalse();
    }
}