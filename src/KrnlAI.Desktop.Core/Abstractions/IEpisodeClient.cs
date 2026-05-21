using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.Core.Abstractions;

public interface IEpisodeClient
{
    Task<EpisodeSearchResult> SearchEpisodesAsync(EpisodeSearchRequest request, CancellationToken cancellationToken = default);
    Task<EpisodeDetails?> GetEpisodeAsync(string episodeId, CancellationToken cancellationToken = default);
}
