using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class PoliciesViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    public ObservableCollection<PolicyInfo> PolicyList { get; } = new();
    public ObservableCollection<string> PolicyDomains { get; } = new() { "", "general", "payments", "infrastructure", "security", "support", "analytics" };
    private string? _selectedDomain;
    public string? SelectedPolicyDomain { get => _selectedDomain; set => SetProperty(ref _selectedDomain, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    private PolicyVersionList? _versions;
    public PolicyVersionList? PolicyVersions { get => _versions; set => SetProperty(ref _versions, value); }
    public ObservableCollection<PolicyRollbackEntry> PolicyRollbacks { get; } = new();
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ICommand LoadPoliciesCommand { get; }
    public ICommand LoadVersionsCommand { get; }
    public ICommand LoadRollbacksCommand { get; }
    public ICommand ClearVersionsCommand { get; }

    public PoliciesViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadPoliciesCommand = new AsyncRelayCommand(LoadAsync);
        LoadVersionsCommand = new AsyncRelayCommand(async () => { if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return; PolicyVersions = await _kernelClient.GetPolicyVersionsAsync(SelectedPolicyDomain ?? ""); });
        LoadRollbacksCommand = new AsyncRelayCommand(async () => { if (ServiceLocator.Instance.CurrentMode == RunMode.Local) return; var l = await _kernelClient.GetPolicyRollbacksAsync(SelectedPolicyDomain ?? ""); PolicyRollbacks.Clear(); foreach (var r in l) PolicyRollbacks.Add(r); });
        ClearVersionsCommand = new RelayCommand(() => PolicyVersions = null);
    }

    public PoliciesViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponível no modo Local";
                return;
            }
            var r = await _kernelClient.GetPoliciesAsync(null, 1, 100);
            PolicyList.Clear();
            if (r?.Policies != null) foreach (var p in r.Policies) PolicyList.Add(p);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar políticas: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
