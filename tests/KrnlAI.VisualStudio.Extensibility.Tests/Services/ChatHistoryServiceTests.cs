using FluentAssertions;
using KrnlAI.VisualStudio.Extensibility.Core.Services;
using KrnlAI.VisualStudio.Extensibility.Core.Services.Settings;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests.Services;

public sealed class ChatHistoryServiceTests
{
    [Fact]
    public void SaveAndLoad_ShouldRoundTripMessages()
    {
        var service = new ChatHistoryService(new MemorySettingsStore());
        var messages = new List<ChatMessage>
        {
            new("user", "hello", new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            new("assistant", "hi there", new DateTime(2026, 1, 1, 10, 0, 5, DateTimeKind.Utc))
        };

        service.SaveMessages(messages);
        var loaded = service.LoadMessages();

        loaded.Should().HaveCount(2);
        loaded[0].Role.Should().Be("user");
        loaded[0].Content.Should().Be("hello");
        loaded[1].Role.Should().Be("assistant");
    }

    [Fact]
    public void ClearHistory_ShouldRemoveAllMessages()
    {
        var service = new ChatHistoryService(new MemorySettingsStore());
        service.SaveMessages(new List<ChatMessage> { new("user", "x", DateTime.UtcNow) });

        service.ClearHistory();

        service.LoadMessages().Should().BeEmpty();
    }
}