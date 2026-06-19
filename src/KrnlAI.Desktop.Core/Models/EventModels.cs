namespace KrnlAI.Desktop.Core.Models;

public sealed record EventInfo(string EventId, string Type, string Description, string? Source, DateTimeOffset Timestamp, Dictionary<string, object>? Metadata);

public sealed record EventDetail(string EventId, string Type, string Description, string? Source, DateTimeOffset Timestamp, Dictionary<string, object>? Metadata, string? RelatedEntityId, string? RelatedEntityType);
