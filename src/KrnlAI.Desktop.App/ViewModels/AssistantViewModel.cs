using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class AssistantViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _content = "", _errorMessage = "";
    private bool _isLoading;
    private ThreadInfo? _activeThread;
    private RunInfo? _activeRun;

    public string Content { get => _content; set => SetProperty(ref _content, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public ThreadInfo? ActiveThread { get => _activeThread; set { SetProperty(ref _activeThread, value); OnPropertyChanged(nameof(IsThreadSelected)); } }
    public RunInfo? ActiveRun { get => _activeRun; set => SetProperty(ref _activeRun, value); }
    public bool IsThreadSelected => ActiveThread != null;

    public ObservableCollection<ThreadInfo> Threads { get; } = [];
    public ObservableCollection<MessageInfo> Messages { get; } = [];

    public ICommand CreateThreadCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand CreateRunCommand { get; }
    public ICommand LoadThreadsCommand { get; }
    public ICommand ClearErrorCommand { get; }

    public AssistantViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        CreateThreadCommand = new AsyncRelayCommand(async param =>
        {
            var title = param as string;
            await CreateThreadAsync(title).ConfigureAwait(false);
        });
        SendMessageCommand = new AsyncRelayCommand(async param =>
        {
            if (param is string content && !string.IsNullOrWhiteSpace(content) && ActiveThread != null)
            {
                Content = content;
                await SendMessageAsync(ActiveThread.ThreadId, content).ConfigureAwait(false);
                Content = "";
            }
        });
        CreateRunCommand = new AsyncRelayCommand(async param =>
        {
            if (param is string threadId)
                await CreateRunAsync(threadId).ConfigureAwait(false);
        });
        LoadThreadsCommand = new AsyncRelayCommand(async _ => await LoadThreadsAsync().ConfigureAwait(false));
        ClearErrorCommand = new RelayCommand(_ => ErrorMessage = "");
    }

    public AssistantViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task CreateThreadAsync(string? title = null)
    {
        ErrorMessage = "";
        try
        {
            var thread = await _kernelClient.CreateThreadAsync(title).ConfigureAwait(false);
            if (thread != null)
            {
                Threads.Add(thread);
                ActiveThread = thread;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar thread: {ex.Message}";
        }
    }

    public async Task SelectThreadAsync(string threadId)
    {
        ErrorMessage = "";
        try
        {
            var thread = await _kernelClient.GetThreadAsync(threadId).ConfigureAwait(false);
            if (thread != null)
            {
                ActiveThread = thread;
                var messages = await _kernelClient.GetMessagesAsync(threadId).ConfigureAwait(false);
                Messages.Clear();
                foreach (var m in messages) Messages.Add(m);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao selecionar thread: {ex.Message}";
        }
    }

    public async Task SendMessageAsync(string threadId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var message = await _kernelClient.SendMessageAsync(threadId, content).ConfigureAwait(false);
            if (message != null) Messages.Add(message);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao enviar mensagem: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CreateRunAsync(string threadId)
    {
        ErrorMessage = "";
        try
        {
            ActiveRun = await _kernelClient.CreateRunAsync(threadId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao iniciar run: {ex.Message}";
        }
    }

    public async Task GetRunAsync(string threadId, string runId)
    {
        ErrorMessage = "";
        try
        {
            ActiveRun = await _kernelClient.GetRunAsync(threadId, runId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar run: {ex.Message}";
        }
    }

    public async Task LoadThreadsAsync()
    {
        ErrorMessage = "";
        try
        {
            Threads.Clear();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar threads: {ex.Message}";
        }
    }

    public void ClearError() => ErrorMessage = "";
}
