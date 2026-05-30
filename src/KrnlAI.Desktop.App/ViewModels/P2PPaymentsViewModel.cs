using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using Microsoft.Extensions.Logging;

namespace KrnlAI.Desktop.App.ViewModels;

public sealed class P2PPaymentsViewModel : ViewModelBase
{
    private readonly ILogger<P2PPaymentsViewModel> _logger;
    private decimal _earnedCents;
    private decimal _spentCents;
    private decimal _pendingCents;
    private string _selectedMode = "TrackOnly";
    private int _pendingReceiptCount;

    public P2PPaymentsViewModel(ILogger<P2PPaymentsViewModel>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<P2PPaymentsViewModel>.Instance;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<object> Receipts { get; } = [];
    public ObservableCollection<object> Batches { get; } = [];
    public ObservableCollection<string> AvailableModes { get; } = ["Free", "TrackOnly", "SettleOnChain"];

    public string EarnedFormatted => $"${_earnedCents:F2}";
    public string SpentFormatted => $"${_spentCents:F2}";
    public string PendingFormatted => $"${_pendingCents:F2}";

    public string SelectedMode
    {
        get => _selectedMode;
        set
        {
            if (SetProperty(ref _selectedMode, value))
                OnPropertyChanged(nameof(ModeDescription));
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
            _earnedCents = 0;
            _spentCents = 0;
            _pendingCents = 0;
            OnPropertyChanged(nameof(EarnedFormatted));
            OnPropertyChanged(nameof(SpentFormatted));
            OnPropertyChanged(nameof(PendingFormatted));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh P2P payments");
        }
    }
}
