using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class EmotionalViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly string _userId;
    private EmotionalState? _currentState;
    private string _errorMessage = "";
    private bool _isLoading;

    public EmotionalState? CurrentState { get => _currentState; set { SetProperty(ref _currentState, value); OnPropertyChanged(nameof(ValenceLabel)); OnPropertyChanged(nameof(ArousalLabel)); OnPropertyChanged(nameof(MoodIcon)); } }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public string ValenceLabel => CurrentState == null ? "—" : CurrentState.Valence switch
    {
        > 0.3 => "Positivo",
        < -0.3 => "Negativo",
        _ => "Neutro"
    };

    public string ArousalLabel => CurrentState == null ? "—" : CurrentState.Arousal switch
    {
        > 0.5 => "Alto",
        < 0.3 => "Baixo",
        _ => "Médio"
    };

    public string MoodIcon => CurrentState == null ? "🧐" : (CurrentState.Valence, CurrentState.Arousal) switch
    {
        (> 0.3, > 0.5) => "⚡",
        (> 0.3, _) => "😊",
        (< -0.3, > 0.5) => "😰",
        (< -0.3, _) => "😔",
        (_, > 0.5) => "🧐",
        _ => "😐"
    };

    public ObservableCollection<EmotionalHistoryEntry> History { get; } = [];

    public ICommand LoadCurrentStateCommand { get; }
    public ICommand LoadHistoryCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public EmotionalViewModel(IKernelClient kernelClient, string? userId = null)
    {
        _kernelClient = kernelClient;
        _userId = userId ?? "default";
        LoadCurrentStateCommand = new AsyncRelayCommand(LoadCurrentStateAsync);
        LoadHistoryCommand = new AsyncRelayCommand(LoadHistoryAsync);
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
    }

    public EmotionalViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadCurrentStateAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            CurrentState = await _kernelClient.GetEmotionalStateAsync(_userId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar estado emocional: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadHistoryAsync()
    {
        ErrorMessage = "";
        try
        {
            var entries = await _kernelClient.EmotionalHistoryAsync();
            History.Clear();
            foreach (var e in entries) History.Add(e);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar histórico: {ex.Message}";
        }
    }

    public async Task<bool> LogEventAsync(string eventName, string? trigger = null, double? valenceDelta = null, double? arousalDelta = null)
    {
        try
        {
            return await _kernelClient.EmotionalEventAsync(eventName, trigger, valenceDelta, arousalDelta);
        }
        catch
        {
            return false;
        }
    }

    public void ClearError() => ErrorMessage = "";
}
