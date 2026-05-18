namespace KrnlAI.Desktop.Tests.Models;

public class MemorySearchResultTests
{
    [Fact]
    public void MemorySearchResult_ShouldStoreValues()
    {
        var hits = new List<Core.Models.MemoryHit> { new("h1", "content", "web", 0.95, DateTime.UtcNow, null) };
        var result = new Core.Models.MemorySearchResult(hits, 1, 0.5);

        Assert.Single(result.Hits);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(0.5, result.QueryTimeMs);
    }

    [Fact]
    public void MemorySearchResult_ShouldAllowEmpty()
    {
        var result = new Core.Models.MemorySearchResult(new List<Core.Models.MemoryHit>(), 0, 0);
        Assert.Empty(result.Hits);
    }
}

public class MemoryHitTests
{
    [Fact]
    public void MemoryHit_ShouldCreateWithCorrectProperties()
    {
        var now = DateTime.UtcNow;
        var meta = new Dictionary<string, string> { { "key", "val" } };
        var hit = new Core.Models.MemoryHit("h1", "text", "doc", 0.85, now, meta);

        Assert.Equal("h1", hit.Id);
        Assert.Equal("text", hit.Content);
        Assert.Equal(0.85, hit.Score);
        Assert.Equal(now, hit.CreatedAt);
        Assert.Equal("val", hit.Metadata!["key"]);
    }

    [Fact]
    public void MemoryHit_ShouldAllowNullMetadata()
    {
        var hit = new Core.Models.MemoryHit("h1", "text", "doc", 0.5, DateTime.UtcNow, null);
        Assert.Null(hit.Metadata);
    }
}

public class MemoryIngestTests
{
    [Fact]
    public void MemoryIngestRequest_ShouldCreateWithContent()
    {
        var req = new Core.Models.MemoryIngestRequest("content", "source", "txt", null);
        Assert.Equal("content", req.Content);
        Assert.Equal("source", req.Source);
        Assert.Equal("txt", req.Type);
    }

    [Fact]
    public void MemoryIngestRequest_ShouldAllowDefaults()
    {
        var req = new Core.Models.MemoryIngestRequest("content");
        Assert.Equal("content", req.Content);
        Assert.Null(req.Source);
        Assert.Null(req.Type);
    }

    [Fact]
    public void MemoryIngestResult_ShouldStoreValues()
    {
        var result = new Core.Models.MemoryIngestResult(true, "doc1", 5, null);
        Assert.True(result.Success);
        Assert.Equal("doc1", result.DocumentId);
        Assert.Equal(5, result.ChunksCreated);
    }

    [Fact]
    public void MemoryIngestResult_ShouldSupportError()
    {
        var result = new Core.Models.MemoryIngestResult(false, null, 0, "error");
        Assert.False(result.Success);
        Assert.Equal("error", result.Error);
    }
}

public class MemoryMetricsTests
{
    [Fact]
    public void MemoryMetrics_ShouldStoreValues()
    {
        var bySource = new Dictionary<string, int> { { "web", 10 }, { "api", 5 } };
        var metrics = new Core.Models.MemoryMetrics(100, 20, 1024, bySource, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        Assert.Equal(100, metrics.TotalChunks);
        Assert.Equal(20, metrics.TotalDocuments);
        Assert.Equal(2, metrics.BySource.Count);
    }
}

public class WorkingMemoryTests
{
    [Fact]
    public void WorkingMemorySlot_ShouldCreate()
    {
        var now = DateTime.UtcNow;
        var slot = new Core.Models.WorkingMemorySlot("key1", "content", 0.9, now, now.AddHours(1));

        Assert.Equal("key1", slot.Key);
        Assert.Equal(0.9, slot.Relevance);
        Assert.NotNull(slot.ExpiresAt);
    }

    [Fact]
    public void WorkingMemorySummary_ShouldStoreSlots()
    {
        var slots = new List<Core.Models.WorkingMemorySlot>
        {
            new("k1", "c1", 0.8, DateTime.UtcNow, null),
            new("k2", "c2", 0.5, DateTime.UtcNow, null)
        };
        var summary = new Core.Models.WorkingMemorySummary(2, 7, slots);

        Assert.Equal(2, summary.ActiveSlots);
        Assert.Equal(7, summary.MaxSlots);
        Assert.Equal(2, summary.Slots.Count);
    }
}
