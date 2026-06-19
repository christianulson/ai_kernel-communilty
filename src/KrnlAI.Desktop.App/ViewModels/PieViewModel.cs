using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class PieViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _premise = "", _context = "", _conclusion = "", _errorMessage = "";
    private string _chainPremise = "", _chainContext = "";
    private int _chainSteps = 3;
    private double _confidence;
    private bool _isLoading;
    private PieCoherenceData? _coherence;

    public string Premise { get => _premise; set => SetProperty(ref _premise, value); }
    public string Context { get => _context; set => SetProperty(ref _context, value); }
    public string Conclusion { get => _conclusion; set => SetProperty(ref _conclusion, value); }
    public double Confidence { get => _confidence; set => SetProperty(ref _confidence, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string ChainPremise { get => _chainPremise; set => SetProperty(ref _chainPremise, value); }
    public string ChainContext { get => _chainContext; set => SetProperty(ref _chainContext, value); }
    public int ChainSteps { get => _chainSteps; set => SetProperty(ref _chainSteps, value); }
    public PieCoherenceData? Coherence { get => _coherence; set => SetProperty(ref _coherence, value); }

    public ObservableCollection<PieChainStep> ChainResults { get; } = [];
    public ObservableCollection<PieTerm> Terms { get; } = [];

    public ICommand InferCommand { get; }
    public ICommand ChainCommand { get; }
    public ICommand LoadTermsCommand { get; }
    public ICommand LoadCoherenceCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public PieViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        InferCommand = new AsyncRelayCommand(InferAsync);
        ChainCommand = new AsyncRelayCommand(ChainAsync);
        LoadTermsCommand = new AsyncRelayCommand(LoadTermsAsync);
        LoadCoherenceCommand = new AsyncRelayCommand(LoadCoherenceAsync);
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
    }

    public PieViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task InferAsync()
    {
        if (string.IsNullOrWhiteSpace(Premise)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var r = await _kernelClient.PieInferAsync(Premise, Context);
            if (r != null)
            {
                Conclusion = r.Conclusion;
                Confidence = r.Confidence;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro na inferência: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ChainAsync()
    {
        if (string.IsNullOrWhiteSpace(ChainPremise)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var r = await _kernelClient.PieChainAsync(ChainPremise, ChainSteps, ChainContext);
            ChainResults.Clear();
            if (r?.Steps != null)
                foreach (var s in r.Steps)
                    ChainResults.Add(s);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro na cadeia: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadTermsAsync()
    {
        ErrorMessage = "";
        try
        {
            var terms = await _kernelClient.PieTermsAsync();
            Terms.Clear();
            foreach (var t in terms) Terms.Add(t);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar termos: {ex.Message}";
        }
    }

    public async Task LoadCoherenceAsync()
    {
        ErrorMessage = "";
        try
        {
            Coherence = await _kernelClient.PieCoherenceAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar coerência: {ex.Message}";
        }
    }

    public async Task<bool> LearnFactAsync(string domain, string fact, double certainty = 1.0)
    {
        try
        {
            var r = await _kernelClient.PieKnowledgeAsync(domain, fact, certainty);
            return r?.Success ?? false;
        }
        catch
        {
            return false;
        }
    }

    public void ClearError() => ErrorMessage = "";
}
