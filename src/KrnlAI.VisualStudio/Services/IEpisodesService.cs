namespace KrnlAI.VisualStudio.Services;

public sealed record Episode(
    string Id,
    string Goal,
    string Status,
    DateTime Timestamp,
    int StepCount,
    TimeSpan? Duration,
    IReadOnlyList<EpisodeStep>? Steps
);

public sealed record EpisodeStep(
    int Number,
    string Tool,
    string? Result,
    bool Success
);

public interface IEpisodesService
{
    Task<IReadOnlyList<Episode>> GetEpisodesAsync(CancellationToken ct);
    Task<Episode?> GetEpisodeDetailsAsync(string episodeId, CancellationToken ct);
}
