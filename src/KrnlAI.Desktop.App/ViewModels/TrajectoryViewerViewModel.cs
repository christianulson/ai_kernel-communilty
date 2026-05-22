using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed record TrajectorySessionSummary
{
    public string Id { get; init; } = "";
    public string Prompt { get; init; } = "";
    public DateTime StartedAt { get; init; }
    public string Status { get; init; } = "";
    public int TotalSteps { get; init; }
    public int TotalTokens { get; init; }
    public string DisplayText => $"{Prompt[..Math.Min(Prompt.Length, 60)]}... ({StartedAt:HH:mm} - {Status})";
}

public sealed record TrajectoryStep
{
    public int Sequence { get; init; }
    public string Stage { get; init; } = "";
    public string Action { get; init; } = "";
    public long DurationMs { get; init; }
    public int TokensUsed { get; init; }
    public double Cost { get; init; }
    public string? Error { get; init; }
    public string? PolicyDecision { get; init; }
}

public sealed record TrajectorySessionDetail
{
    public string Id { get; init; } = "";
    public string? UserId { get; init; }
    public string Prompt { get; init; } = "";
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public string Status { get; init; } = "";
    public int TotalSteps { get; init; }
    public int TotalTokens { get; init; }
    public double TotalCost { get; init; }
    public string? Mode { get; init; }
    public List<TrajectoryStep> Steps { get; init; } = new();
}

public sealed class TrajectoryViewerViewModel : ViewModelBase
{
    private readonly HttpClient _http;
    private int _selectedTabIndex;
    private string _searchText = "";

    public ObservableCollection<TrajectorySessionSummary> Sessions { get; } = new();
    private TrajectorySessionDetail? _selectedSession;
    public TrajectorySessionDetail? SelectedSession
    {
        get => _selectedSession;
        set { SetProperty(ref _selectedSession, value); OnPropertyChanged(nameof(HasSelection)); }
    }
    public bool HasSelection => SelectedSession != null;

    public int SelectedTabIndex { get => _selectedTabIndex; set => SetProperty(ref _selectedTabIndex, value); }
    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); _ = LoadSessionsAsync(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand LoadSessionCommand { get; }

    public TrajectoryViewerViewModel()
    {
        _http = new HttpClient { BaseAddress = new Uri("http://localhost:5000"), Timeout = TimeSpan.FromSeconds(10) };
        RefreshCommand = new AsyncRelayCommand(LoadSessionsAsync);
        LoadSessionCommand = new AsyncRelayCommand(async () =>
        {
            if (Sessions.FirstOrDefault() is TrajectorySessionSummary s)
                await LoadSessionAsync(s.Id);
        });
        _ = LoadSessionsAsync();
    }

    public async Task LoadSessionsAsync()
    {
        try
        {
            var url = "/api/trajectories?page=1&pageSize=50";
            if (!string.IsNullOrWhiteSpace(SearchText))
                url += "&search=" + Uri.EscapeDataString(SearchText);

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var items = await response.Content.ReadFromJsonAsync<List<TrajectorySessionSummary>>();
            Sessions.Clear();
            if (items != null)
                foreach (var item in items) Sessions.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load trajectories: {ex.Message}");
        }
    }

    public async Task LoadSessionAsync(string sessionId)
    {
        try
        {
            var response = await _http.GetAsync($"/api/trajectories/{sessionId}");
            response.EnsureSuccessStatusCode();
            var session = await response.Content.ReadFromJsonAsync<TrajectorySessionDetail>();
            SelectedSession = session;
            SelectedTabIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load trajectory: {ex.Message}");
        }
    }
}
