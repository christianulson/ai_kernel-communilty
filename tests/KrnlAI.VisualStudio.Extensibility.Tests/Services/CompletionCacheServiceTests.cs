using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Services;

public sealed class CompletionCacheServiceTests
{
    [Fact]
    public void SetAndGet_ShouldReturnCachedValue()
    {
        var cache = new CompletionCacheService();

        cache.Set("ctx1", new CachedCompletion("completion", DateTime.UtcNow));
        var result = cache.Get("ctx1");

        result.Should().NotBeNull();
        result!.Text.Should().Be("completion");
    }

    [Fact]
    public void Get_MissingKey_ShouldReturnNull()
    {
        var cache = new CompletionCacheService();

        cache.Get("missing").Should().BeNull();
    }

    [Fact]
    public void Set_OverCapacity_ShouldEvictLeastRecentlyUsed()
    {
        var cache = new CompletionCacheService(maxEntries: 2);

        cache.Set("a", new CachedCompletion("1", DateTime.UtcNow));
        cache.Set("b", new CachedCompletion("2", DateTime.UtcNow));
        cache.Get("a");
        cache.Set("c", new CachedCompletion("3", DateTime.UtcNow));

        cache.Get("b").Should().BeNull();
        cache.Get("a").Should().NotBeNull();
    }

    [Fact]
    public void Get_ExpiredEntry_ShouldReturnNull()
    {
        var cache = new CompletionCacheService(ttl: TimeSpan.Zero);

        cache.Set("a", new CachedCompletion("1", DateTime.UtcNow));

        cache.Get("a").Should().BeNull();
    }
}