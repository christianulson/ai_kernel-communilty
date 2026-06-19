using KrnlAI.Contracts.Contracts;

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

public record LoginRequest(
    string Email,
    string Password
);

public record LoginResponse(
    bool Success,
    string? Token = null,
    string? Message = null,
    string? Username = null,
    string? RefreshToken = null
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