using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class DocumentViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    public ObservableCollection<DocumentInfo> DocumentList { get; } = new();
    private DocumentInfo? _selected;
    public DocumentInfo? SelectedDocument { get => _selected; set => SetProperty(ref _selected, value); }
    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(HasNoData)); } }
    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasNoData => !IsLoading && DocumentList.Count == 0 && !HasError;
    public ICommand LoadDocumentsCommand { get; }
    public ICommand ClearSelectionCommand { get; }

    public DocumentViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadDocumentsCommand = new AsyncRelayCommand(LoadAsync);
        ClearSelectionCommand = new RelayCommand(() => SelectedDocument = null);
    }

    public DocumentViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            if (ServiceLocator.Instance.CurrentMode == RunMode.Local)
            {
                ErrorMessage = "Indisponível no modo Local";
                return;
            }
            var docs = await _kernelClient.GetDocumentsAsync(50);
            DocumentList.Clear();
            if (docs != null)
                foreach (var d in docs) DocumentList.Add(d);
            OnPropertyChanged(nameof(HasNoData));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar documentos: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
