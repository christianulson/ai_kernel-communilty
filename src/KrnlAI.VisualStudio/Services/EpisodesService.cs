using System.Net.Http.Json;
using System.Text.Json;

namespace KrnlAI.VisualStudio.Services;

public sealed class EpisodesService : IEpisodesService, IDisposable
{
    private readonly HttpClient _http;
    private string? _baseUrl;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private IReadOnlyList<Episode>? _cached;
    private DateTime _lastFetch = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public EpisodesService(HttpClient? http = null)
    {
        _http = http ?? new HttpClient();
    }

    private string GetBaseUrl()
    {
        if (_baseUrl is not null) return _baseUrl;
        try
        {
            var settings = new SettingsService();
            settings.Load();
            _baseUrl = KernelEndpointResolver.Resolve(settings.RuntimeMode, settings.Endpoint, settings.SidecarPort);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            _baseUrl = "http://localhost:65335";
        }
        return _baseUrl;
    }

    public async Task<IReadOnlyList<Episode>> GetEpisodesAsync(CancellationToken ct)
    {
        if (_cached is not null && DateTime.UtcNow - _lastFetch < CacheTtl)
            return _cached;

        try
        {
            var response = await _http.GetAsync($"{GetBaseUrl()}/episodes/search?pageSize=50", ct);
            if (!response.IsSuccessStatusCode)
                return _cached ?? Array.Empty<Episode>();

            var result = await response.Content
                .ReadFromJsonAsync<EpisodeSearchDto>(JsonOpts, ct);

            if (result is not null)
            {
                _cached = result.Episodes.Select(MapEpisode).ToList();
                _lastFetch = DateTime.UtcNow;
            }

            return _cached ?? Array.Empty<Episode>();
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return _cached ?? Array.Empty<Episode>();
        }
    }

    public async Task<Episode?> GetEpisodeDetailsAsync(string episodeId, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync(
                $"{GetBaseUrl()}/episodes/{episodeId}", ct);
            if (!response.IsSuccessStatusCode) return null;

            var dto = await response.Content
                .ReadFromJsonAsync<EpisodeDetailDto>(JsonOpts, ct);
            return dto is null ? null : MapEpisode(dto);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    public void Dispose()
    {
        _http.Dispose();
    }

    private static Episode MapEpisode(EpisodeDto dto)
    {
        var duration = dto.Duration is not null
            ? dto.Duration
            : dto.DurationMs.HasValue ? TimeSpan.FromMilliseconds(dto.DurationMs.Value) : null;
        return new Episode(
            dto.Id,
            dto.Goal ?? dto.GoalId ?? dto.Summary ?? "",
            dto.Status,
            dto.Timestamp ?? dto.CreatedAt,
            dto.StepCount ?? dto.Steps?.Count ?? 0,
            duration,
            dto.Steps);
    }

    private sealed record EpisodeSearchDto(List<EpisodeDto> Episodes, int TotalCount);

    private record EpisodeDto(
        string Id,
        string? Goal,
        string? GoalId,
        string Status,
        DateTime CreatedAt,
        DateTime? Timestamp,
        int? StepCount,
        int? DurationMs,
        TimeSpan? Duration,
        string? Summary,
        List<EpisodeStep>? Steps);

    private sealed record EpisodeDetailDto(
        string Id,
        string? Goal,
        string? GoalId,
        string Status,
        DateTime CreatedAt,
        DateTime? Timestamp,
        int? StepCount,
        int? DurationMs,
        TimeSpan? Duration,
        string? Summary,
        List<EpisodeStep>? Steps) : EpisodeDto(Id, Goal, GoalId, Status, CreatedAt, Timestamp, StepCount, DurationMs, Duration, Summary, Steps);
}
