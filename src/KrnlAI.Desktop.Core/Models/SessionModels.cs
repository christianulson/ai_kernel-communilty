namespace KrnlAI.Desktop.Core.Models;

public record SessionShare(string ShareCode, string SessionId, string AccessLevel, DateTime CreatedAt, DateTime? ExpiresAt, int AccessCount, bool IsRevoked);

public record ShareListResponse(IReadOnlyList<SessionShare> Shares);
