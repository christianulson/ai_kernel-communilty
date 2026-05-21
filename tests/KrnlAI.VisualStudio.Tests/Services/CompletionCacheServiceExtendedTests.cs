using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class CompletionCacheServiceExtendedTests
{
    [Fact]
    public void Get_ShouldUpdateAccessOrder()
    {
        var cache = new CompletionCacheService(maxEntries: 3);
        cache.Set("a", new CachedCompletion("p", new[] { "1" }, new[] { 1.0 }, DateTime.UtcNow));
        cache.Set("b", new CachedCompletion("p", new[] { "2" }, new[] { 1.0 }, DateTime.UtcNow));
        cache.Set("c", new CachedCompletion("p", new[] { "3" }, new[] { 1.0 }, DateTime.UtcNow));

        cache.Get("a"); // Access 'a' — moves to front
        cache.Set("d", new CachedCompletion("p", new[] { "4" }, new[] { 1.0 }, DateTime.UtcNow)); // 'b' is evicted (LRU)

        cache.Get("a").Should().NotBeNull();
        cache.Get("b").Should().BeNull();
        cache.Get("c").Should().NotBeNull();
        cache.Get("d").Should().NotBeNull();
    }

    [Fact]
    public void Set_ExistingKey_ShouldUpdate()
    {
        var cache = new CompletionCacheService();
        cache.Set("k", new CachedCompletion("p", new[] { "old" }, new[] { 0.5 }, DateTime.UtcNow));
        cache.Set("k", new CachedCompletion("p", new[] { "new" }, new[] { 0.9 }, DateTime.UtcNow));

        var result = cache.Get("k");
        result.Should().NotBeNull();
        result!.Suggestions[0].Should().Be("new");
        result.Scores[0].Should().Be(0.9);
    }

    [Fact]
    public void Count_ShouldReflectCacheSize()
    {
        var cache = new CompletionCacheService(maxEntries: 100);
        cache.Set("a", new CachedCompletion("p", new[] { "1" }, new[] { 1.0 }, DateTime.UtcNow));
        cache.Set("b", new CachedCompletion("p", new[] { "2" }, new[] { 1.0 }, DateTime.UtcNow));
        cache.Count.Should().Be(2);
    }

    [Fact]
    public void Clear_ShouldRemoveAll()
    {
        var cache = new CompletionCacheService();
        cache.Set("a", new CachedCompletion("p", new[] { "1" }, new[] { 1.0 }, DateTime.UtcNow));
        cache.Clear();
        cache.Count.Should().Be(0);
        cache.Get("a").Should().BeNull();
    }
}
