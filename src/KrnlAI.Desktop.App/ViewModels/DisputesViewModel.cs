using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed record DisputeItem(string DisputeId, string WorkId, string WorkerNodeId, string Reason, string Status, DateTime OpenedAt);

public sealed class DisputesViewModel : ViewModelBase
{
    private readonly ILogger<DisputesViewModel> _logger;
    private readonly HttpClient _http;
    private DisputeItem? _selectedDispute;

    public DisputesViewModel(ILogger<DisputesViewModel>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DisputesViewModel>.Instance;
        var baseUrl = Environment.GetEnvironmentVariable("KRNL__API_BASE_URL") ?? "http://localhost:5235";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(10) };
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ResolveForWorkerCommand = new AsyncRelayCommand(() => ResolveAsync("worker"));
        ResolveForSolicitorCommand = new AsyncRelayCommand(() => ResolveAsync("solicitor"));
    }

    public ObservableCollection<DisputeItem> Disputes { get; } = [];

    public DisputeItem? SelectedDispute
    {
        get => _selectedDispute;
        set
        {
            SetProperty(ref _selectedDispute, value);
            OnPropertyChanged(nameof(HasSelectedDispute));
        }
    }

    public bool HasSelectedDispute => _selectedDispute != null;

    public ICommand RefreshCommand { get; }
    public ICommand ResolveForWorkerCommand { get; }
    public ICommand ResolveForSolicitorCommand { get; }

    private async Task RefreshAsync()
    {
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;

            var items = await _http.GetFromJsonAsync<List<DisputeItem>>("/api/disputes");
            Disputes.Clear();
            if (items != null)
                foreach (var d in items) Disputes.Add(d);
        }
        catch (HttpRequestException) when (ServiceLocator.Instance.CurrentMode == RunMode.Api)
        {
            _logger.LogWarning("Disputes API not available on this backend");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh disputes");
        }
    }

    private async Task ResolveAsync(string favor)
    {
        if (_selectedDispute == null) return;
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Api)
            {
                await _http.PostAsJsonAsync($"/api/disputes/{_selectedDispute.DisputeId}/resolve", new { favor });
            }
            SelectedDispute = null;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve dispute {DisputeId}", _selectedDispute.DisputeId);
        }
    }
}
