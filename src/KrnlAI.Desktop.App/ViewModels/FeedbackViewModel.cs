using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class FeedbackViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _errorMessage = "";
    private bool _isLoading;
    private int _rating = 5;
    private string _comment = "";
    private string _category = "";
    private FeedbackAverage? _average;

    public ObservableCollection<FeedbackHistoryEntry> History { get; } = [];

    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public int Rating { get => _rating; set => SetProperty(ref _rating, value); }
    public string Comment { get => _comment; set => SetProperty(ref _comment, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); }
    public FeedbackAverage? Average { get => _average; set => SetProperty(ref _average, value); }

    public ICommand LoadHistoryCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand ClearErrorCommand { get; }
    public ICommand SetRating1Command { get; }
    public ICommand SetRating2Command { get; }
    public ICommand SetRating3Command { get; }
    public ICommand SetRating4Command { get; }
    public ICommand SetRating5Command { get; }

    public FeedbackViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadHistoryCommand = new AsyncRelayCommand(LoadHistoryAsync);
        SubmitCommand = new AsyncRelayCommand(async _ => await SubmitAsync().ConfigureAwait(false));
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
        SetRating1Command = new RelayCommand(() => Rating = 1);
        SetRating2Command = new RelayCommand(() => Rating = 2);
        SetRating3Command = new RelayCommand(() => Rating = 3);
        SetRating4Command = new RelayCommand(() => Rating = 4);
        SetRating5Command = new RelayCommand(() => Rating = 5);
    }

    public FeedbackViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var history = await _kernelClient.GetFeedbackHistoryAsync().ConfigureAwait(false);
            History.Clear();
            if (history != null)
                foreach (var entry in history)
                    History.Add(entry);

            Average = await _kernelClient.GetFeedbackAverageAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar histórico: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> SubmitAsync()
    {
        ErrorMessage = "";
        try
        {
            var request = new FeedbackRequest("", Rating, string.IsNullOrWhiteSpace(Comment) ? null : Comment, string.IsNullOrWhiteSpace(Category) ? null : Category);
            var response = await _kernelClient.SubmitFeedbackAsync(request).ConfigureAwait(false);
            if (response.Success)
            {
                Rating = 5;
                Comment = "";
                Category = "";
                await LoadHistoryAsync().ConfigureAwait(false);
                return true;
            }
            ErrorMessage = response.Message ?? "Falha ao enviar feedback";
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao enviar feedback: {ex.Message}";
            return false;
        }
    }
}
