using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;

namespace KrnlAI.Desktop.App.ViewModels;

public class CognitiveCycleViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly ICognitiveStreamProvider _streamProvider;
    private CognitiveStreamState _streamState = CognitiveStreamState.Disconnected;
    private string _selectedCycleId = "";
    private string _errorMessage = "";
    private bool _isLoading;

    public CognitiveStreamState StreamState { get => _streamState; set => SetProperty(ref _streamState, value); }
    public string SelectedCycleId { get => _selectedCycleId; set => SetProperty(ref _selectedCycleId, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool IsConnected => StreamState == CognitiveStreamState.Connected;

    public ObservableCollection<CognitiveCycleEvent> Events { get; } = [];

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand ClearEventsCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public CognitiveCycleViewModel(IKernelClient kernelClient, ICognitiveStreamProvider streamProvider)
    {
        _kernelClient = kernelClient;
        _streamProvider = streamProvider;
        ConnectCommand = new AsyncRelayCommand(async _ => await ConnectAsync().ConfigureAwait(false));
        DisconnectCommand = new RelayCommand(() => Disconnect());
        ClearEventsCommand = new RelayCommand(() => Events.Clear());
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");

        _streamProvider.OnEvent += OnStreamEvent;
        _streamProvider.OnStateChanged += OnStreamStateChanged;
    }

    public CognitiveCycleViewModel()
        : this(
            ServiceLocator.Instance.KernelClient,
            ServiceLocator.Instance.CognitiveStreamProvider)
    { }

    public async Task ConnectAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var cycleId = string.IsNullOrWhiteSpace(SelectedCycleId) ? null : SelectedCycleId;
            await _streamProvider.ConnectAsync(cycleId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao conectar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Disconnect()
    {
        _streamProvider.Disconnect();
    }

    private void OnStreamEvent(CognitiveCycleEvent evt)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Events.Insert(0, evt);
            if (Events.Count > 500)
                Events.RemoveAt(Events.Count - 1);
        });
    }

    private void OnStreamStateChanged(CognitiveStreamState state)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => StreamState = state);
    }

    public void Cleanup()
    {
        _streamProvider.OnEvent -= OnStreamEvent;
        _streamProvider.OnStateChanged -= OnStreamStateChanged;
        _streamProvider.Disconnect();
    }
}
