using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class BenchmarkViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
    private BenchmarkSummary? _data;
    public BenchmarkSummary? Data { get => _data; set => SetProperty(ref _data, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public ICommand LoadCommand { get; }

    public BenchmarkViewModel()
    {
        _services = ServiceLocator.Instance;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            Data = await _services.KernelClient.GetBenchmarkSummaryAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar benchmark: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
