using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class CreativityViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _prompt = "";
    private string _result = "";
    private string _errorMessage = "";
    private bool _isLoading;
    private double _confidence;

    public string Prompt { get => _prompt; set => SetProperty(ref _prompt, value); }
    public string Result { get => _result; set { SetProperty(ref _result, value); OnPropertyChanged(nameof(HasResult)); } }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasResult => !string.IsNullOrEmpty(Result);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public double Confidence { get => _confidence; set => SetProperty(ref _confidence, value); }

    public ObservableCollection<PieChainStep> ChainSteps { get; } = [];

    public ICommand GenerateCommand { get; }
    public ICommand ChainCommand { get; }
    public ICommand ClearErrorCommand { get; }
    public ICommand ClearResultCommand { get; }

    public CreativityViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        GenerateCommand = new AsyncRelayCommand(async _ => await GenerateAsync());
        ChainCommand = new AsyncRelayCommand(async _ => await ChainAsync());
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
        ClearResultCommand = new RelayCommand(() => { Result = ""; ChainSteps.Clear(); });
    }

    public CreativityViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(Prompt)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var result = await _kernelClient.PieInferAsync(Prompt);
            if (result != null)
            {
                Result = result.Conclusion;
                Confidence = result.Confidence;
            }
            else
            {
                ErrorMessage = "Falha ao gerar ideia criativa";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ChainAsync()
    {
        if (string.IsNullOrWhiteSpace(Prompt)) return;
        IsLoading = true;
        ErrorMessage = "";
        ChainSteps.Clear();
        try
        {
            var result = await _kernelClient.PieChainAsync(Prompt, 5);
            if (result != null)
            {
                foreach (var step in result.Steps)
                    ChainSteps.Add(step);
                if (result.Steps.Count > 0)
                {
                    var last = result.Steps[^1];
                    Result = last.Conclusion;
                    Confidence = last.Confidence;
                }
            }
            else
            {
                ErrorMessage = "Falha ao encadear ideias";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
