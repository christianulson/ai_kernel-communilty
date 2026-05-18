using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class PoliciesViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
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

    public PoliciesViewModel()
    {
        _services = ServiceLocator.Instance;
        LoadPoliciesCommand = new AsyncRelayCommand(LoadAsync);
        LoadVersionsCommand = new AsyncRelayCommand(async () => PolicyVersions = await _services.KernelClient.GetPolicyVersionsAsync(SelectedPolicyDomain ?? ""));
        LoadRollbacksCommand = new AsyncRelayCommand(async () => { var l = await _services.KernelClient.GetPolicyRollbacksAsync(SelectedPolicyDomain ?? ""); PolicyRollbacks.Clear(); foreach (var r in l) PolicyRollbacks.Add(r); });
        ClearVersionsCommand = new RelayCommand(() => PolicyVersions = null);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var r = await _services.KernelClient.GetPoliciesAsync(null, 1, 100);
            PolicyList.Clear();
            foreach (var p in r.Policies) PolicyList.Add(p);
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
