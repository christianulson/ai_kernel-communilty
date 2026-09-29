using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class CodingViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _code = "", _language = "", _description = "", _testFramework = "", _errorMessage = "";
    private bool _isLoading;
    private CodingResponse? _result;
    private CodingStatus? _status;

    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string Language { get => _language; set => SetProperty(ref _language, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string TestFramework { get => _testFramework; set => SetProperty(ref _testFramework, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public CodingResponse? Result { get => _result; set => SetProperty(ref _result, value); }
    public CodingStatus? Status { get => _status; set => SetProperty(ref _status, value); }

    public ICommand ExplainCommand { get; }
    public ICommand FixCommand { get; }
    public ICommand GenerateTestsCommand { get; }
    public ICommand ReviewCommand { get; }
    public ICommand ApplyDiffCommand { get; }
    public ICommand CompleteCommand { get; }
    public ICommand LoadStatusCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public CodingViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        ExplainCommand = new AsyncRelayCommand(async _ => await ExplainAsync());
        FixCommand = new AsyncRelayCommand(async _ => await FixAsync());
        GenerateTestsCommand = new AsyncRelayCommand(async _ => await GenerateTestsAsync());
        ReviewCommand = new AsyncRelayCommand(async _ => await ReviewAsync());
        ApplyDiffCommand = new AsyncRelayCommand(async _ => await ApplyDiffAsync());
        CompleteCommand = new AsyncRelayCommand(async _ => await CompleteAsync());
        LoadStatusCommand = new AsyncRelayCommand(async param =>
        {
            if (param is string cycleId && !string.IsNullOrWhiteSpace(cycleId))
                await LoadStatusAsync(cycleId);
        });
        ClearErrorCommand = new RelayCommand(_ => ErrorMessage = "");
    }

    public CodingViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task ExplainAsync()
    {
        if (string.IsNullOrWhiteSpace(Code)) return;
        await ExecuteCodingAsync(() => _kernelClient.CodingExplainAsync(BuildRequest()));
    }

    public async Task FixAsync()
    {
        if (string.IsNullOrWhiteSpace(Code)) return;
        await ExecuteCodingAsync(() => _kernelClient.CodingFixAsync(BuildRequest()));
    }

    public async Task GenerateTestsAsync()
    {
        if (string.IsNullOrWhiteSpace(Code)) return;
        await ExecuteCodingAsync(() => _kernelClient.CodingGenerateTestsAsync(BuildRequest()));
    }

    public async Task ReviewAsync()
    {
        if (string.IsNullOrWhiteSpace(Code)) return;
        await ExecuteCodingAsync(() => _kernelClient.CodingReviewAsync(BuildRequest()));
    }

    public async Task ApplyDiffAsync()
    {
        if (string.IsNullOrWhiteSpace(Code)) return;
        await ExecuteCodingAsync(() => _kernelClient.CodingApplyDiffAsync(BuildRequest()));
    }

    public async Task CompleteAsync()
    {
        if (string.IsNullOrWhiteSpace(Code)) return;
        await ExecuteCodingAsync(() => _kernelClient.CodingCompleteAsync(BuildRequest()));
    }

    public async Task LoadStatusAsync(string cycleId)
    {
        ErrorMessage = "";
        try
        {
            Status = await _kernelClient.GetCodingStatusAsync(cycleId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar status: {ex.Message}";
        }
    }

    private CodingRequest BuildRequest() => new(Code, string.IsNullOrWhiteSpace(Language) ? null : Language, string.IsNullOrWhiteSpace(Description) ? null : Description, string.IsNullOrWhiteSpace(TestFramework) ? null : TestFramework);

    private async Task ExecuteCodingAsync(Func<Task<CodingResponse?>> operation)
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            Result = await operation();
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

    public void ClearError() => ErrorMessage = "";
}
