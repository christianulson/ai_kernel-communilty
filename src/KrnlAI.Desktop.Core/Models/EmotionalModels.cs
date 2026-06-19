namespace KrnlAI.Desktop.Core.Models;

public sealed record EmotionalHistoryEntry(DateTimeOffset Timestamp, string Event, double Valence, double Arousal, string? Trigger);

public sealed record EmotionalEventRequest(string Event, string? Trigger, double? ValenceDelta, double? ArousalDelta);
