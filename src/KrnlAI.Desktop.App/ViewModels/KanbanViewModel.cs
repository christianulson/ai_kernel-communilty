using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.ViewModels;

public class KanbanViewModel : ViewModelBase
{
    private readonly KanbanService _kanbanService;
    private bool _isLoading;
    private int _daysBack = 10;
    private string? _selectedDomain;
    private double _minPriority;
    private string _searchText = "";
    private string? _errorMessage;

    public ObservableCollection<KanbanColumnDisplay> Columns { get; } = [];
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public int DaysBack { get => _daysBack; set { if (SetProperty(ref _daysBack, value)) _ = LoadAsync(); } }
    public string? SelectedDomain { get => _selectedDomain; set { if (SetProperty(ref _selectedDomain, value)) _ = LoadAsync(); } }
    public double MinPriority { get => _minPriority; set { if (SetProperty(ref _minPriority, value)) _ = LoadAsync(); } }
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public string? ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public ICommand LoadCommand { get; }
    public ICommand MoveCardCommand { get; }
    public ICommand SearchCommand { get; }

    public KanbanViewModel() : this(ServiceLocator.Instance.KanbanService) { }

    public KanbanViewModel(KanbanService kanbanService)
    {
        _kanbanService = kanbanService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        MoveCardCommand = new AsyncRelayCommand(async p =>
        {
            if (p is Tuple<string, string> args)
                await MoveCardInnerAsync(args.Item1, args.Item2);
        });
        SearchCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var data = await _kanbanService.GetKanbanAsync(
                DaysBack, SelectedDomain,
                MinPriority > 0 ? MinPriority : null,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
            Columns.Clear();
            foreach (var col in data.Columns)
                Columns.Add(col);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task MoveCardInnerAsync(string cardId, string toColumn)
    {
        try
        {
            var ok = await _kanbanService.MoveCardAsync(cardId, toColumn);
            if (ok) await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
