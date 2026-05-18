using KrnlAI.Cli.Tui;

namespace KrnlAI.Cli.Tests;

public sealed class TuiSessionStoreTests : IDisposable
{
    private readonly string _tmpDir;

    public TuiSessionStoreTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "krnlai-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tmpDir, true); }
        catch { /* best effort cleanup */ }
    }

    private TuiSessionStore CreateStore() => new(_tmpDir);

    [Fact]
    public async Task TuiSessionStore_SaveAndList_ShouldReturnSession()
    {
        var store = CreateStore();
        var messages = new List<ChatMessage>
        {
            new("user", "hello", false, DateTimeOffset.UtcNow),
            new("assistant", "world", false, DateTimeOffset.UtcNow),
        };

        await store.SaveAsync("test session", messages);
        var sessions = await store.ListAsync();

        Assert.NotEmpty(sessions);
        Assert.Contains(sessions, s => s.Label == "test session" && s.MessageCount == 2);
    }

    [Fact]
    public async Task TuiSessionStore_LoadSession_ShouldReturnMessages()
    {
        var store = new TuiSessionStore();
        var messages = new List<ChatMessage>
        {
            new("user", "test content", false, DateTimeOffset.UtcNow),
        };

        await store.SaveAsync("load test", messages);
        var sessions = await store.ListAsync();
        Assert.NotEmpty(sessions);

        var loaded = await store.LoadAsync(sessions[0].Id);
        Assert.NotNull(loaded);
        Assert.Equal("load test", loaded.Label);
        Assert.Single(loaded.Messages);
        Assert.Equal("test content", loaded.Messages[0].Content);
    }

    [Fact]
    public async Task TuiSessionStore_LoadNonexistent_ShouldReturnNull()
    {
        var store = new TuiSessionStore();
        var result = await store.LoadAsync("nonexistent-id");
        Assert.Null(result);
    }

    [Fact]
    public async Task TuiSessionStore_DeleteSession_ShouldRemove()
    {
        var store = new TuiSessionStore();
        await store.SaveAsync("delete me", []);
        var sessions = await store.ListAsync();
        var id = sessions[0].Id;

        var deleted = await store.DeleteAsync(id);
        Assert.True(deleted);

        var afterDelete = await store.ListAsync();
        Assert.DoesNotContain(afterDelete, s => s.Id == id);
    }

    [Fact]
    public async Task TuiSessionStore_ExportSession_ShouldReturnJSON()
    {
        var store = new TuiSessionStore();
        await store.SaveAsync("export test", [new("user", "data", false, DateTimeOffset.UtcNow)]);
        var sessions = await store.ListAsync();

        var json = await store.ExportAsync(sessions[0].Id);
        Assert.False(string.IsNullOrEmpty(json));
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json.Trim());
    }

    [Fact]
    public async Task TuiSessionStore_ImportSession_ShouldRestore()
    {
        var store = new TuiSessionStore();
        var json = @"{
            ""Id"": ""imported"",
            ""Label"": ""imported session"",
            ""CreatedAt"": ""2026-05-16T12:00:00Z"",
            ""MessageCount"": 1,
            ""Messages"": [{ ""Role"": ""user"", ""Content"": ""imported msg"", ""IsError"": false, ""Timestamp"": ""2026-05-16T12:00:00Z"" }]
        }";

        var result = await store.ImportAsync(json);
        Assert.NotNull(result);
        Assert.Equal("imported session", result.Label);

        var sessions = await store.ListAsync();
        Assert.Contains(sessions, s => s.Label == "imported session");
    }

    [Fact]
    public async Task TuiSessionStore_ImportInvalidJSON_ShouldReturnNull()
    {
        var store = new TuiSessionStore();
        var result = await store.ImportAsync("{invalid}");
        Assert.Null(result);
    }
}
