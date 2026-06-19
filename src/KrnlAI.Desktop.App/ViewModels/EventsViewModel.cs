using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class EventsViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _errorMessage = "", _typeFilter = "";
    private bool _isLoading;
    private EventDetail? _selectedEvent;
    private string _momentId = "";

    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public EventDetail? SelectedEvent { get => _selectedEvent; set => SetProperty(ref _selectedEvent, value); }
    public string MomentId { get => _momentId; set => SetProperty(ref _momentId, value); }

    public ObservableCollection<EventInfo> Events { get; } = [];
    public ObservableCollection<EventInfo> FilteredEvents { get; } = [];

    public ICommand LoadRecentCommand { get; }
    public ICommand LoadByMomentCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public EventsViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadRecentCommand = new AsyncRelayCommand(LoadRecentEventsAsync);
        LoadByMomentCommand = new AsyncRelayCommand(async () =>
        {
            if (!string.IsNullOrWhiteSpace(MomentId))
                await LoadEventsByMomentAsync(MomentId);
        });
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
    }

    public EventsViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadRecentEventsAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var events = await _kernelClient.EventsRecentAsync(50);
            Events.Clear();
            foreach (var e in events) Events.Add(e);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar eventos: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadEventDetailAsync(string eventId)
    {
        ErrorMessage = "";
        try
        {
            SelectedEvent = await _kernelClient.EventDetailAsync(eventId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar detalhe: {ex.Message}";
        }
    }

    public async Task LoadEventsByMomentAsync(string momentId)
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var events = await _kernelClient.EventsByMomentAsync(momentId);
            Events.Clear();
            foreach (var e in events) Events.Add(e);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar eventos do momento: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SetTypeFilter(string typeFilter)
    {
        _typeFilter = typeFilter;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredEvents.Clear();
        var filtered = string.IsNullOrWhiteSpace(_typeFilter)
            ? Events
            : new ObservableCollection<EventInfo>(
                Events.Where(e => e.Type.Equals(_typeFilter, StringComparison.OrdinalIgnoreCase)));
        foreach (var e in filtered) FilteredEvents.Add(e);
    }

    public void ClearError() => ErrorMessage = "";
}
