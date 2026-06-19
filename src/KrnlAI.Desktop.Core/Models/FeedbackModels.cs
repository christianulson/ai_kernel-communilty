namespace KrnlAI.Desktop.Core.Models;

public record FeedbackRequest(
    string EpisodeId,
    int Rating,
    string? Comment,
    string? Category
);

public record FeedbackResponse(
    bool Success,
    string? FeedbackId,
    string? Message
);

public record FeedbackAverage(
    int TotalFeedbacks,
    double AverageRating,
    int Rating1Count,
    int Rating2Count,
    int Rating3Count,
    int Rating4Count,
    int Rating5Count
);

public record FeedbackHistoryEntry(
    string FeedbackId,
    string EpisodeId,
    int Rating,
    string? Comment,
    string? Category,
    DateTimeOffset CreatedAt
);
