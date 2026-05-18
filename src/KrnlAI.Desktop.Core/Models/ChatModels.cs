namespace KrnlAI.Desktop.Core.Models;

public record ChatMessage(
    string Id,
    string Content,
    MessageRole Role,
    DateTime Timestamp,
    MessageStatus Status = MessageStatus.Pending,
    string? ErrorMessage = null,
    string? ImageBase64 = null
);

public enum MessageRole
{
    User,
    Assistant,
    System
}

public enum MessageStatus
{
    Pending,
    Processing,
    Completed,
    Error
}

public record ChatSession(
    string Id,
    List<ChatMessage> Messages,
    DateTime CreatedAt,
    DateTime? LastActivityAt
);

public record AgentRunRequest(
    string Prompt,
    string Mode = "gateway",
    string? AgentId = null,
    Dictionary<string, string>? Metadata = null,
    byte[]? ImageBytes = null,
    string? ImageFormat = null
);

public record AgentRunResponse(
    string? Narration,
    Dictionary<string, object>? Command,
    List<TransportStep>? TransportSteps,
    List<string>? ActiveStages,
    string? Error
);

public record TransportStep(
    string Label,
    string Detail,
    bool Ok,
    int? Status
);

public record LoginRequest(
    string Username,
    string Password
);

public record LoginResponse(
    bool Success,
    string? Token = null,
    string? Message = null,
    string? Username = null,
    DateTime? ExpiresAt = null
);

public record SpeechRequest(string Text, string Language, string? Voice);

public record TranscribeRequest(string audio_base64, string language);

public record MultimodalSearchRequest(string query, int topK);

public record AuthSettings(
    string Token,
    string? Username,
    DateTime? ExpiresAt
);

public record EmotionalState(
    double Valence,
    double Arousal,
    double Motivation,
    DateTimeOffset UpdatedAt
);