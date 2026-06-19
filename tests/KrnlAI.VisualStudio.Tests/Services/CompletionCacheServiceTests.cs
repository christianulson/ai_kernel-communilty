using KrnlAI.VisualStudio.Services;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

public sealed class CompletionCacheServiceTests
{
    [Fact]
    public void Get_MissingKey_ShouldReturnNull()
    {
        var cache = new CompletionCacheService();

        var result = cache.Get("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void SetAndGet_ShouldReturnValue()
    {
        var cache = new CompletionCacheService();
        var completion = new CachedCompletion("prefix", ["suggestion1"], [0.9], DateTime.UtcNow);

        cache.Set("hash1", completion);
        var result = cache.Get("hash1");

        result.Should().NotBeNull();
        result!.Prefix.Should().Be("prefix");
        result.Suggestions.Should().Contain("suggestion1");
        result.Scores[0].Should().Be(0.9);
    }

    [Fact]
    public void Invalidate_ShouldRemoveEntry()
    {
        var cache = new CompletionCacheService();
        cache.Set("hash1", new CachedCompletion("p", ["s"], [1.0], DateTime.UtcNow));

        cache.Invalidate("hash1");

        cache.Get("hash1").Should().BeNull();
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void Clear_ShouldRemoveAll()
    {
        var cache = new CompletionCacheService();
        cache.Set("hash1", new CachedCompletion("p", ["s"], [1.0], DateTime.UtcNow));
        cache.Set("hash2", new CachedCompletion("p", ["s"], [1.0], DateTime.UtcNow));

        cache.Clear();

        cache.Count.Should().Be(0);
        cache.Get("hash1").Should().BeNull();
        cache.Get("hash2").Should().BeNull();
    }

    [Fact]
    public void Set_OverMaxEntries_ShouldEvictLRU()
    {
        var cache = new CompletionCacheService(maxEntries: 2);
        cache.Set("a", new CachedCompletion("p", ["s"], [1.0], DateTime.UtcNow));
        cache.Set("b", new CachedCompletion("p", ["s"], [1.0], DateTime.UtcNow));
        cache.Set("c", new CachedCompletion("p", ["s"], [1.0], DateTime.UtcNow));

        cache.Count.Should().Be(2);
        cache.Get("a").Should().BeNull();
        cache.Get("b").Should().NotBeNull();
        cache.Get("c").Should().NotBeNull();
    }

    [Fact]
    public void Get_ExpiredEntry_ShouldReturnNull()
    {
        var cache = new CompletionCacheService(maxEntries: 100, ttl: TimeSpan.FromMilliseconds(-1));
        cache.Set("hash1", new CachedCompletion("p", ["s"], [1.0], DateTime.UtcNow.AddDays(-1)));

        var result = cache.Get("hash1");

        result.Should().BeNull();
        cache.Count.Should().Be(0);
    }
}
