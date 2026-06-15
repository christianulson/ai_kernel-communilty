using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed record P2PReceipt(string WorkId, string WorkerNodeId, string Type, decimal PriceCents, bool Success, DateTime ExecutedAt);
public sealed record P2PBatch(string BatchId, ObservableCollection<P2PReceipt> Receipts, decimal TotalCents, string Status, DateTime CreatedAt);

/// <summary>View model for the P2P payments page, displaying balance, receipts, and payment mode.</summary>
public sealed class P2PPaymentsViewModel : ViewModelBase
{
    private readonly ILogger<P2PPaymentsViewModel> _logger;
    private readonly HttpClient _http;
    private decimal _earnedCents;
    private decimal _spentCents;
    private decimal _pendingCents;
    private string _selectedMode = "TrackOnly";
    private int _pendingReceiptCount;

    public P2PPaymentsViewModel(ILogger<P2PPaymentsViewModel>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<P2PPaymentsViewModel>.Instance;
        var settings = ServiceLocator.Instance.SettingsService.LoadSettings();
        var baseUrl = settings.ApiEndpoint ?? settings.ApiBaseUrl ?? Environment.GetEnvironmentVariable("KRNL__API_BASE_URL") ?? "http://localhost:5235";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(10) };
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<P2PReceipt> Receipts { get; } = [];
    public ObservableCollection<P2PBatch> Batches { get; } = [];
    public ObservableCollection<string> AvailableModes { get; } = ["Free", "TrackOnly", "SettleOnChain"];

    public string EarnedFormatted => (_earnedCents / 100.0m).ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
    public string SpentFormatted => (_spentCents / 100.0m).ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
    public string PendingFormatted => (_pendingCents / 100.0m).ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));

    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
            {
                OnPropertyChanged(nameof(ModeDescription));
                _ = SaveModeAsync();
            }
        }
    }

    public int PendingReceiptCount
    {
        get => _pendingReceiptCount;
        set => SetProperty(ref _pendingReceiptCount, value);
    }

    public string ModeDescription => SelectedMode switch
    {
        "Free" => "Modo gratuito — não cobra nem paga por processamento remoto.",
        "TrackOnly" => "Apenas rastreio — registra créditos mas não realiza settlement.",
        "SettleOnChain" => "Settlement on-chain — realiza liquidação financeira via blockchain L2.",
        _ => ""
    };

    public ICommand RefreshCommand { get; }

    private async Task RefreshAsync()
    {
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return;

            var receiptsTask = _http.GetFromJsonAsync<List<P2PReceipt>>("/api/p2p/receipts");
            var batchesTask = _http.GetFromJsonAsync<List<P2PBatchDto>>("/api/p2p/batches");
            var summaryTask = _http.GetFromJsonAsync<P2PSummaryDto>("/api/p2p/summary");
            await Task.WhenAll(receiptsTask, batchesTask, summaryTask);

            Receipts.Clear();
            if (receiptsTask.Result != null)
                foreach (var r in receiptsTask.Result) Receipts.Add(r);

            Batches.Clear();
            if (batchesTask.Result != null)
            {
                foreach (var b in batchesTask.Result)
                {
                    var batchReceipts = new ObservableCollection<P2PReceipt>(b.Receipts ?? []);
                    Batches.Add(new P2PBatch(b.BatchId, batchReceipts, b.TotalCents, b.Status ?? "unknown", b.CreatedAt));
                }
            }

            if (summaryTask.Result != null)
            {
                _earnedCents = summaryTask.Result.EarnedCents;
                _spentCents = summaryTask.Result.SpentCents;
                _pendingCents = summaryTask.Result.PendingCents;
                PendingReceiptCount = summaryTask.Result.PendingCount;
            }

            OnPropertyChanged(nameof(EarnedFormatted));
            OnPropertyChanged(nameof(SpentFormatted));
            OnPropertyChanged(nameof(PendingFormatted));
        }
        catch (HttpRequestException) when (ServiceLocator.Instance.CurrentMode == RunMode.Api)
        {
            _logger.LogWarning("P2P API not available on this backend");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh P2P payments");
        }
    }

    private async Task SaveModeAsync()
    {
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Api)
                await _http.PostAsJsonAsync("/api/p2p/mode", new { mode = _selectedMode });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save P2P mode");
        }
    }

    private sealed record P2PSummaryDto(decimal EarnedCents, decimal SpentCents, decimal PendingCents, int PendingCount);
    private sealed record P2PBatchDto(string BatchId, List<P2PReceipt>? Receipts, decimal TotalCents, string? Status, DateTime CreatedAt);
}
