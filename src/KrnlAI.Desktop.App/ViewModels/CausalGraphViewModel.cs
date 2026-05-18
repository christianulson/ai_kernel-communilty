using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class CausalGraphViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;
    private string _tab = "query", _query = "", _predictAction = "", _errorMessage = "";
    public string Query { get => _query; set => SetProperty(ref _query, value); }
    public string PredictAction { get => _predictAction; set => SetProperty(ref _predictAction, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public string Tab { get => _tab; set { if (SetProperty(ref _tab, value)) { OnPropertyChanged(nameof(IsQuery)); OnPropertyChanged(nameof(IsPredict)); } } }
    public bool IsQuery => _tab == "query";
    public bool IsPredict => _tab == "predict";

    private CausalQueryResult? _resultData;
    public CausalQueryResult? ResultData { get => _resultData; set => SetProperty(ref _resultData, value); }
    private CausalPrediction? _predictionResult;
    public CausalPrediction? PredictionResult { get => _predictionResult; set => SetProperty(ref _predictionResult, value); }

    private bool _isSearching;
    public bool IsSearching { get => _isSearching; set { SetProperty(ref _isSearching, value); OnPropertyChanged(nameof(SearchButtonText)); } }
    public string SearchButtonText => IsSearching ? "Analisando..." : "Analisar";

    private bool _isPredicting;
    public bool IsPredicting { get => _isPredicting; set { SetProperty(ref _isPredicting, value); OnPropertyChanged(nameof(PredictButtonText)); } }
    public string PredictButtonText => IsPredicting ? "Prevendo..." : "Predizer";

    public ICommand SearchCommand { get; }
    public ICommand PredictCommand { get; }
    public ICommand SetQueryTabCommand { get; }
    public ICommand SetPredictTabCommand { get; }

    public CausalGraphViewModel()
    {
        _services = ServiceLocator.Instance;
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        PredictCommand = new AsyncRelayCommand(PredictAsync);
        SetQueryTabCommand = new RelayCommand(() => Tab = "query");
        SetPredictTabCommand = new RelayCommand(() => Tab = "predict");
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;
        IsSearching = true;
        ErrorMessage = "";
        try
        {
            ResultData = await _services.KernelClient.GetCausalQueryAsync(Query);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro na consulta causal: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task PredictAsync()
    {
        if (string.IsNullOrWhiteSpace(PredictAction)) return;
        IsPredicting = true;
        ErrorMessage = "";
        try
        {
            PredictionResult = await _services.KernelClient.GetCausalPredictionAsync(PredictAction);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro na predição: {ex.Message}";
        }
        finally
        {
            IsPredicting = false;
        }
    }
}
