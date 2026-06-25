using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

/// <summary>
/// View model for the peer ranking dashboard.
/// </summary>
public sealed class PeerRankingViewModel : ViewModelBase
{
    private readonly IPeerRankingManagementService _service;
    private readonly ILogger<PeerRankingViewModel> _logger;
    private readonly List<PeerRankingItem> _allPeers = [];
    private bool _isBusy;
    private string _statusMessage = "Pronto.";
    private string _errorMessage = string.Empty;
    private string _filterText = string.Empty;
    private string _selectedTierFilter = "All";
    private string _selectedStrategy = "TopRanked";
    private PeerRankingItem? _selectedPeer;
    private double _successRateWeight = 0.35;
    private double _latencyWeight = 0.20;
    private double _availabilityWeight = 0.20;
    private double _tenureWeight = 0.05;
    private double _capacityWeight = 0.10;
    private double _catalogWeight = 0.10;

    public PeerRankingViewModel()
        : this(ServiceLocator.Instance.PeerRankingManagementService, ServiceLocator.Instance.GetLogger<PeerRankingViewModel>())
    {
    }

    public PeerRankingViewModel(IPeerRankingManagementService service, ILogger<PeerRankingViewModel>? logger = null)
    {
        _service = service;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PeerRankingViewModel>.Instance;

        LoadCommand = new AsyncRelayCommand(() => LoadAsync());
        RefreshCommand = new AsyncRelayCommand(() => LoadAsync());
        SaveWeightsCommand = new AsyncRelayCommand(() => SaveWeightsAsync());
        SaveStrategyCommand = new AsyncRelayCommand(() => SaveStrategyAsync());
        RecomputeCommand = new AsyncRelayCommand(() => RecomputeAsync());
    }

    public ObservableCollection<PeerRankingItem> FilteredPeers { get; } = [];

    public ObservableCollection<PeerRankingHistoryEntry> History { get; } = [];

    public ObservableCollection<string> AvailableStrategies { get; } = [];

    public IReadOnlyList<string> AvailableTiers { get; } = ["All", "Untrusted", "Standard", "Trusted", "Preferred"];

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                ApplyFilters();
        }
    }

    public string SelectedTierFilter
    {
        get => _selectedTierFilter;
        set
        {
            if (SetProperty(ref _selectedTierFilter, value))
                ApplyFilters();
        }
    }

    public string SelectedStrategy
    {
        get => _selectedStrategy;
        set => SetProperty(ref _selectedStrategy, value);
    }

    public PeerRankingItem? SelectedPeer
    {
        get => _selectedPeer;
        set => SetProperty(ref _selectedPeer, value);
    }

    public double SuccessRateWeight { get => _successRateWeight; set => SetProperty(ref _successRateWeight, value); }
    public double LatencyWeight { get => _latencyWeight; set => SetProperty(ref _latencyWeight, value); }
    public double AvailabilityWeight { get => _availabilityWeight; set => SetProperty(ref _availabilityWeight, value); }
    public double TenureWeight { get => _tenureWeight; set => SetProperty(ref _tenureWeight, value); }
    public double CapacityWeight { get => _capacityWeight; set => SetProperty(ref _capacityWeight, value); }
    public double CatalogWeight { get => _catalogWeight; set => SetProperty(ref _catalogWeight, value); }

    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SaveWeightsCommand { get; }
    public ICommand SaveStrategyCommand { get; }
    public ICommand RecomputeCommand { get; }

    public Task LoadAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            await ReloadPeersAsync(ct).ConfigureAwait(false);
            await ReloadWeightsAsync(ct).ConfigureAwait(false);
            await ReloadStrategyAsync(ct).ConfigureAwait(false);

            SelectedPeer ??= FilteredPeers.FirstOrDefault();
            if (SelectedPeer is not null)
                await LoadHistoryAsync(SelectedPeer.NodeId, ct).ConfigureAwait(false);

            StatusMessage = $"Peers carregados: {FilteredPeers.Count}.";
            ErrorMessage = string.Empty;
        });

    public Task LoadHistoryAsync(string? nodeId, CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            History.Clear();
            if (string.IsNullOrWhiteSpace(nodeId))
                return;

            var history = await _service.GetHistoryAsync(nodeId, ct).ConfigureAwait(false);
            foreach (var item in history)
                History.Add(item);
        });

    public Task SaveWeightsAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            await _service.UpdateWeightsAsync(BuildWeights(), ct).ConfigureAwait(false);
            await ReloadWeightsAsync(ct).ConfigureAwait(false);
            StatusMessage = "Pesos atualizados.";
            ErrorMessage = string.Empty;
        });

    public Task SaveStrategyAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            await _service.UpdateStrategyAsync(SelectedStrategy, ct).ConfigureAwait(false);
            await ReloadStrategyAsync(ct).ConfigureAwait(false);
            StatusMessage = "Estratégia atualizada.";
            ErrorMessage = string.Empty;
        });

    public Task RecomputeAsync(CancellationToken ct = default)
        => ExecuteBusyAsync(async () =>
        {
            var updated = await _service.RecomputeAsync(ct).ConfigureAwait(false);
            StatusMessage = $"Recomputados {updated} peers.";
            await ReloadPeersAsync(ct).ConfigureAwait(false);
            if (SelectedPeer is not null)
                await LoadHistoryAsync(SelectedPeer.NodeId, ct).ConfigureAwait(false);
            ErrorMessage = string.Empty;
        });

    private async Task ReloadPeersAsync(CancellationToken ct)
    {
        var ranking = await _service.GetRankingAsync(ct).ConfigureAwait(false);
        _allPeers.Clear();
        _allPeers.AddRange(ranking);
        ApplyFilters();
    }

    private async Task ReloadWeightsAsync(CancellationToken ct)
    {
        var weights = await _service.GetWeightsAsync(ct).ConfigureAwait(false);
        SuccessRateWeight = weights.SuccessRateWeight;
        LatencyWeight = weights.LatencyWeight;
        AvailabilityWeight = weights.AvailabilityWeight;
        TenureWeight = weights.TenureWeight;
        CapacityWeight = weights.CapacityWeight;
        CatalogWeight = weights.CatalogWeight;
    }

    private async Task ReloadStrategyAsync(CancellationToken ct)
    {
        var strategy = await _service.GetStrategyAsync(ct).ConfigureAwait(false);
        AvailableStrategies.Clear();
        foreach (var item in strategy.AvailableStrategies)
            AvailableStrategies.Add(item);

        if (!AvailableStrategies.Contains(strategy.CurrentStrategyName))
            AvailableStrategies.Add(strategy.CurrentStrategyName);

        SelectedStrategy = strategy.CurrentStrategyName;
    }

    private void ApplyFilters()
    {
        var filtered = _allPeers
            .Where(peer =>
                (string.IsNullOrWhiteSpace(FilterText) || peer.NodeId.Contains(FilterText, StringComparison.OrdinalIgnoreCase)) &&
                (string.Equals(SelectedTierFilter, "All", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(peer.Tier, SelectedTierFilter, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(peer => peer.OverallScore)
            .ThenBy(peer => peer.NodeId)
            .ToList();

        FilteredPeers.Clear();
        foreach (var item in filtered)
            FilteredPeers.Add(item);
    }

    private PeerRankingWeights BuildWeights()
        => new(SuccessRateWeight, LatencyWeight, AvailabilityWeight, TenureWeight, CapacityWeight, CatalogWeight);

    private async Task ExecuteBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Peer ranking dashboard operation failed");
            ErrorMessage = ex.Message;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
