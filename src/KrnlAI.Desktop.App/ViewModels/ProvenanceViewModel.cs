using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KrnlAI.Desktop.App.ViewModels;

public class ProvenanceViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ILogger<ProvenanceViewModel> _logger;
    private List<ProvenanceEntry>? _entries;
    public List<ProvenanceEntry>? Entries { get => _entries; set { SetProperty(ref _entries, value); OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(HasNoData)); } }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    private bool _isIntact;
    public bool IsIntact { get => _isIntact; set { SetProperty(ref _isIntact, value); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasData => Entries?.Count > 0;
    public bool HasNoData => !IsLoading && !HasError && !HasData;
    public ICommand LoadCommand { get; }
    public ICommand VerifyCommand { get; }
    public string? SearchEntityId { get; set; }

    public ProvenanceViewModel(IKernelClient kernelClient, ILogger<ProvenanceViewModel>? logger = null)
    {
        _kernelClient = kernelClient;
        _logger = logger ?? NullLogger<ProvenanceViewModel>.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        VerifyCommand = new AsyncRelayCommand(async p => { if (p is string id) await VerifyAsync(id); });
    }

    public ProvenanceViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true; ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local) { ErrorMessage = "Indisponivel no modo Local"; return; }
            var entityId = SearchEntityId ?? "latest";
            Entries = await _kernelClient.GetChainAsync(entityId).ConfigureAwait(false);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load provenance"); ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    public async Task VerifyAsync(string entityId)
    {
        try
        {
            IsIntact = await _kernelClient.VerifyChainAsync(entityId).ConfigureAwait(false);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to verify {Id}", entityId); ErrorMessage = ex.Message; }
    }
}
