using System.IO;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using Xunit;

namespace KrnlAI.Desktop.Tests;

public sealed class SessionPersistenceServiceTests
{
    [Fact]
    public void CreateNewConversation_ShouldGenerateId()
    {
        var service = new SessionPersistenceService(Path.GetTempPath());

        var conv = service.CreateNewConversation("Test Chat");

        Assert.NotNull(conv.Id);
        Assert.Equal("Test Chat", conv.Title);
        Assert.Empty(conv.Messages);
    }

    [Fact]
    public void RenameConversation_ShouldUpdateTitle()
    {
        var service = new SessionPersistenceService(Path.GetTempPath());
        var conv = service.CreateNewConversation("Old Title");

        var renamed = service.RenameConversation(conv, "New Title");

        Assert.Equal("New Title", renamed.Title);
        Assert.Equal(conv.Id, renamed.Id);
    }

    [Fact]
    public void SaveAndLoad_ShouldRoundTrip()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new SessionPersistenceService(tempDir);

        var conv = service.CreateNewConversation("Test");
        var store = new SessionStore(2, new List<ConversationData> { conv }, conv.Id);
        service.Save(store);

        var loaded = service.Load();
        Assert.Single(loaded.Conversations);
        Assert.Equal("Test", loaded.Conversations[0].Title);
        Assert.Equal(conv.Id, loaded.ActiveConversationId);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Load_WhenNoFile_ShouldReturnEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var service = new SessionPersistenceService(tempDir);

        var store = service.Load();

        Assert.Empty(store.Conversations);
        Assert.Null(store.ActiveConversationId);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void DeleteConversation_ShouldRemove()
    {
        var service = new SessionPersistenceService(Path.GetTempPath());
        var conv = service.CreateNewConversation("To Delete");
        var conv2 = service.CreateNewConversation("Keep");
        var store = new SessionStore(2, new List<ConversationData> { conv, conv2 }, conv.Id);

        var result = service.DeleteConversation(store, conv.Id);

        Assert.Single(result.Conversations);
        Assert.Equal("Keep", result.Conversations[0].Title);
        Assert.Null(result.ActiveConversationId);
    }

    [Fact]
    public void Load_CorruptedJson_ShouldReturnEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "sessions.json");
        File.WriteAllText(filePath, "this is not valid json {{{");
        var service = new SessionPersistenceService(tempDir);

        var store = service.Load();

        Assert.Empty(store.Conversations);
        Assert.Null(store.ActiveConversationId);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Load_EmptyJson_ShouldReturnEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "sessions.json");
        File.WriteAllText(filePath, "{}");
        var service = new SessionPersistenceService(tempDir);

        var store = service.Load();

        Assert.Empty(store.Conversations);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Save_ThenLoadMultiple_ShouldPreserveOrder()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new SessionPersistenceService(tempDir);

        var c1 = service.CreateNewConversation("First");
        var c2 = service.CreateNewConversation("Second");
        var c3 = service.CreateNewConversation("Third");
        var store = new SessionStore(2, new List<ConversationData> { c1, c2, c3 }, c2.Id);
        service.Save(store);

        var loaded = service.Load();
        Assert.Equal(3, loaded.Conversations.Count);
        Assert.Equal("First", loaded.Conversations[0].Title);
        Assert.Equal("Second", loaded.Conversations[1].Title);
        Assert.Equal("Third", loaded.Conversations[2].Title);
        Assert.Equal(c2.Id, loaded.ActiveConversationId);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Migrate_FromV1_ShouldGenerateTitles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "sessions.json");
        var service = new SessionPersistenceService(tempDir);

        var v1Json = "{\"version\":1,\"conversations\":[{\"version\":1,\"id\":\"c1\",\"title\":\"\",\"messages\":[{\"id\":\"m1\",\"content\":\"Hello world\",\"role\":0,\"timestamp\":\"2026-05-20T00:00:00Z\",\"status\":0}],\"createdAt\":\"2026-05-20T00:00:00Z\",\"lastActivityAt\":null}],\"activeConversationId\":\"c1\"}";
        File.WriteAllText(filePath, v1Json);

        var loaded = service.Load();
        Assert.Equal(2, loaded.Version);
        Assert.StartsWith("Hello world", loaded.Conversations[0].Title);

        Directory.Delete(tempDir, true);
    }
}
