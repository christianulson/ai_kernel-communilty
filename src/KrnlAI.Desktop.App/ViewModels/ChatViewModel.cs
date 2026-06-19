using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KrnlAI.Desktop.App.Controls;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using KrnlAI.Embedded;
using SlashCommandInfo = KrnlAI.Desktop.App.Services.SlashCommandInfo;

namespace KrnlAI.Desktop.App.ViewModels;

public class ChatViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private EmbeddedKrnlAI? _embeddedKernel;
    private readonly IAudioCapture _audioCapture;
    private readonly IAudioPlayback _audioPlayback;
    private readonly IVideoCapture _videoCapture;
    private readonly ILocalizationService _localization;
    private readonly ISessionPersistenceService? _sessionStore;
    private string _sessionId = "default";
    private readonly bool _persistenceEnabled;

    // Message history for up/down navigation
    private readonly List<string> _messageHistory = [];
    private int _historyIndex = -1;
    private string _savedInput = string.Empty;
    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KrnlAI", "chat_history.json");

    // Slash commands
    private readonly ISlashCommandExecutor _slashHandler;
    private readonly SlashCommandService _slashCommands = new();
    private ObservableCollection<SlashCommandInfo> _slashSuggestions = [];
    public ObservableCollection<SlashCommandInfo> SlashSuggestions
    {
        get => _slashSuggestions;
        set => SetProperty(ref _slashSuggestions, value);
    }
    private bool _isSlashSuggestionsVisible;
    public bool IsSlashSuggestionsVisible
    {
        get => _isSlashSuggestionsVisible;
        set => SetProperty(ref _isSlashSuggestionsVisible, value);
    }
    private int _slashSelectedIndex;
    public int SlashSelectedIndex
    {
        get => _slashSelectedIndex;
        set => SetProperty(ref _slashSelectedIndex, value);
    }

    // @-mentions
    private static readonly (string Label, string Description, string Icon)[] MentionItems =
    {
        ("@file", "Reference a file by name", "\U0001f4c4"),
        ("@selection", "Current editor selection", "\U0001f4cc"),
        ("@symbol", "Code symbol (class, function)", "\U0001f523"),
        ("@terminal", "Terminal output", "\U0001f4bb"),
        ("@diagnostics", "Project errors/warnings", "\u26a0\ufe0f"),
    };

    private ObservableCollection<string> _mentionSuggestions = [];
    public ObservableCollection<string> MentionSuggestions
    {
        get => _mentionSuggestions;
        set => SetProperty(ref _mentionSuggestions, value);
    }
    private bool _isMentionSuggestionsVisible;
    public bool IsMentionSuggestionsVisible
    {
        get => _isMentionSuggestionsVisible;
        set => SetProperty(ref _isMentionSuggestionsVisible, value);
    }
    private int _mentionSelectedIndex;
    public int MentionSelectedIndex
    {
        get => _mentionSelectedIndex;
        set => SetProperty(ref _mentionSelectedIndex, value);
    }

    public ObservableCollection<ChatMessage> Messages { get; } = [];
    public ObservableCollection<ChatMessage> FilteredMessages { get; } = [];
    private string _searchQuery = "";
    public string SearchQuery { get => _searchQuery; set { if (SetProperty(ref _searchQuery, value)) ApplySearchFilter(); } }
    private string _inputText = "";
    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                ((AsyncRelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
                UpdateSlashAndMentionSuggestions();
            }
        }
    }

    private void UpdateSlashAndMentionSuggestions()
    {
        // Check for @-mention
        var lastAtIndex = _inputText.LastIndexOf('@');
        if (lastAtIndex >= 0)
        {
            var afterAt = _inputText[(lastAtIndex + 1)..];
            if (!afterAt.Contains(' ') && !afterAt.Contains('/'))
            {
                var filtered = MentionItems
                    .Where(m => m.Label.Contains(afterAt, StringComparison.OrdinalIgnoreCase))
                    .Select(m => $"{m.Icon} {m.Label} — {m.Description}")
                    .ToList();
                MentionSuggestions = new ObservableCollection<string>(filtered);
                IsMentionSuggestionsVisible = filtered.Count > 0;
                MentionSelectedIndex = filtered.Count > 0 ? 0 : -1;
                IsSlashSuggestionsVisible = false;
                return;
            }
        }
        IsMentionSuggestionsVisible = false;

        // Check for slash command
        if (_inputText.StartsWith("/") && !_inputText.Contains(' '))
        {
            var filtered = _slashCommands.Filter(_inputText);
            SlashSuggestions = new ObservableCollection<SlashCommandInfo>(filtered);
            IsSlashSuggestionsVisible = filtered.Count > 0;
            SlashSelectedIndex = filtered.Count > 0 ? 0 : -1;
        }
        else
        {
            IsSlashSuggestionsVisible = false;
        }
    }

    public void SelectNextMentionSuggestion()
    {
        if (MentionSuggestions.Count == 0) return;
        MentionSelectedIndex = (MentionSelectedIndex + 1) % MentionSuggestions.Count;
    }

    public void SelectPreviousMentionSuggestion()
    {
        if (MentionSuggestions.Count == 0) return;
        MentionSelectedIndex = (MentionSelectedIndex - 1 + MentionSuggestions.Count) % MentionSuggestions.Count;
    }

    public void ApplyMentionSuggestion()
    {
        if (MentionSelectedIndex < 0 || MentionSelectedIndex >= MentionSuggestions.Count) return;
        var selected = MentionSuggestions[MentionSelectedIndex];
        var label = selected.Split(' ')[1]; // "@file"
        var lastAtIndex = InputText.LastIndexOf('@');
        if (lastAtIndex >= 0)
        {
            InputText = InputText[..lastAtIndex] + label + " ";
        }
        IsMentionSuggestionsVisible = false;
    }

    public void SelectNextSlashSuggestion()
    {
        if (SlashSuggestions.Count == 0) return;
        SlashSelectedIndex = (SlashSelectedIndex + 1) % SlashSuggestions.Count;
    }

    public void SelectPreviousSlashSuggestion()
    {
        if (SlashSuggestions.Count == 0) return;
        SlashSelectedIndex = (SlashSelectedIndex - 1 + SlashSuggestions.Count) % SlashSuggestions.Count;
    }

    public void ApplySlashSuggestion()
    {
        if (SlashSelectedIndex < 0 || SlashSelectedIndex >= SlashSuggestions.Count) return;
        var cmd = SlashSuggestions[SlashSelectedIndex];
        InputText = cmd.Command + " ";
        IsSlashSuggestionsVisible = false;
    }
    private bool _isProcessing;
    public bool IsProcessing { get => _isProcessing; set { if (SetProperty(ref _isProcessing, value)) ((AsyncRelayCommand)SendMessageCommand).RaiseCanExecuteChanged(); } }

    public void NavigateHistoryUp()
    {
        if (_messageHistory.Count == 0) return;
        if (_historyIndex == -1) _savedInput = InputText;
        if (_historyIndex < _messageHistory.Count - 1)
        {
            _historyIndex++;
            InputText = _messageHistory[_historyIndex];
        }
    }

    public void NavigateHistoryDown()
    {
        if (_historyIndex == -1) return;
        _historyIndex--;
        InputText = _historyIndex == -1 ? _savedInput : _messageHistory[_historyIndex];
    }

    private void SaveToHistory(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (_messageHistory.Count == 0 || _messageHistory[0] != text)
        {
            _messageHistory.Insert(0, text);
            if (_messageHistory.Count > 50) _messageHistory.RemoveAt(_messageHistory.Count - 1);
        }
        _historyIndex = -1;
        SaveHistoryToDisk();
    }

    private void LoadHistoryFromDisk()
    {
        try
        {
            if (!File.Exists(HistoryPath)) return;
            var json = File.ReadAllText(HistoryPath);
            var list = JsonSerializer.Deserialize<List<string>>(json);
            if (list != null)
            {
                _messageHistory.Clear();
                _messageHistory.AddRange(list);
            }
        }
        catch { /* best-effort */ }
    }

    private void SaveHistoryToDisk()
    {
        try
        {
            var dir = Path.GetDirectoryName(HistoryPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_messageHistory);
            File.WriteAllText(HistoryPath, json);
        }
        catch { /* best-effort */ }
    }

    // Cognitive stream
    private readonly ICognitiveStreamProvider _cognitiveStream;
    private bool _isCognitiveStreamVisible;
    public bool IsCognitiveStreamVisible
    {
        get => _isCognitiveStreamVisible;
        set => SetProperty(ref _isCognitiveStreamVisible, value);
    }
    public List<StageViewModel> CognitiveStages { get; } = [];
    public List<EventViewModel> CognitiveEvents { get; } = [];
    public event Action? CognitiveDataChanged;

    // TTS toggle
    private bool _isTtsEnabled = true;
    public bool IsTtsEnabled
    {
        get => _isTtsEnabled;
        set { SetProperty(ref _isTtsEnabled, value); OnPropertyChanged(nameof(TtsIcon)); }
    }

    public string TtsIcon => IsTtsEnabled ? "\U0001f399\ufe0f" : "\U0001f507";
    public ICommand ToggleTtsCommand { get; }

    // Audio capture
    private bool _isCapturingAudio;
    public bool IsCapturingAudio { get => _isCapturingAudio; set { SetProperty(ref _isCapturingAudio, value); OnPropertyChanged(nameof(AudioButtonIcon)); OnPropertyChanged(nameof(AudioButtonTooltip)); } }
    public string AudioButtonIcon => IsCapturingAudio ? "\U0001f534" : "\U0001f3a4";
    public string AudioButtonTooltip => IsCapturingAudio
        ? (_localization?.GetString("chat_audio_tooltip_stop") ?? "Stop recording")
        : (_localization?.GetString("chat_audio_tooltip_start") ?? "Record audio");

    // Camera
    private bool _isCameraOn;
    public bool IsCameraOn { get => _isCameraOn; set { SetProperty(ref _isCameraOn, value); OnPropertyChanged(nameof(CameraButtonIcon)); } }
    public string CameraButtonIcon => IsCameraOn ? "\U0001f4f9" : "\U0001f4f7";
    private ImageSource? _cameraPreviewSource;
    public ImageSource? CameraPreviewSource { get => _cameraPreviewSource; set => SetProperty(ref _cameraPreviewSource, value); }
    private byte[]? _lastFrameJpeg;
    public bool HasCameraPreview => _lastFrameJpeg != null;

    public ICommand SendMessageCommand { get; }
    public ICommand ClearChatCommand { get; }
    public ICommand ToggleAudioCaptureCommand { get; }
    public ICommand ToggleCameraCommand { get; }
    public ICommand SnapCameraCommand { get; }
    public ICommand DismissCameraPreviewCommand { get; }
    public ICommand ExportConversationCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ShareConversationCommand { get; }
    public ICommand EditMessageCommand { get; }
    public ICommand DeleteMessageCommand { get; }



    public ChatViewModel(IKernelClient kernelClient, IAudioCapture audioCapture, IAudioPlayback audioPlayback, IVideoCapture videoCapture, ILocalizationService localization, ISlashCommandExecutor slashHandler, ICognitiveStreamProvider cognitiveStream, ISessionPersistenceService? sessionStore = null, EmbeddedKrnlAI? embeddedKernel = null)
    {
        _kernelClient = kernelClient;
        _embeddedKernel = embeddedKernel;
        _audioCapture = audioCapture;
        _audioPlayback = audioPlayback;
        _videoCapture = videoCapture;
        _localization = localization;
        _slashHandler = slashHandler;
        _cognitiveStream = cognitiveStream;
        _sessionStore = sessionStore;
        _persistenceEnabled = sessionStore != null;
        ToggleTtsCommand = new RelayCommand(() => IsTtsEnabled = !IsTtsEnabled);
        _cognitiveStream.OnEvent += OnCognitiveEvent;
        _cognitiveStream.OnStateChanged += state => IsCognitiveStreamVisible = state == CognitiveStreamState.Connected;
        LoadHistoryFromDisk();
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, () => !IsProcessing && (!string.IsNullOrWhiteSpace(InputText) || _lastFrameJpeg != null));
        ClearChatCommand = new RelayCommand(() => Messages.Clear());
        ToggleAudioCaptureCommand = new AsyncRelayCommand(ToggleAudioCaptureAsync);
        ToggleCameraCommand = new AsyncRelayCommand(ToggleCameraAsync);
        SnapCameraCommand = new AsyncRelayCommand(SnapCameraAsync, () => _isCameraOn && _lastFrameJpeg != null);
        DismissCameraPreviewCommand = new RelayCommand(DismissCameraPreview);
        ExportConversationCommand = new AsyncRelayCommand(ExportConversationAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
        ShareConversationCommand = new RelayCommand(ShareConversation);
        EditMessageCommand = new AsyncRelayCommand(async p => { if (p is string id) await EditMessageAsync(id); });
        DeleteMessageCommand = new RelayCommand(p => { if (p is string id) DeleteMessage(id); });
    }

    public ChatViewModel() : this(
        ServiceLocator.Instance.KernelClient,
        ServiceLocator.Instance.AudioCapture,
        ServiceLocator.Instance.AudioPlayback,
        ServiceLocator.Instance.VideoCapture,
        ServiceLocator.Instance.LocalizationService,
        ServiceLocator.Instance.SlashCommandExecutor,
        ServiceLocator.Instance.CognitiveStreamProvider,
        new KrnlAI.Desktop.Core.Services.SessionPersistenceService())
    { }

    public string SessionId { get => _sessionId; set { if (_sessionId != value) { _sessionId = value; LoadMessagesFromPersistence(); } } }

    private void LoadMessagesFromPersistence()
    {
        if (!_persistenceEnabled || _sessionStore == null) return;
        try
        {
            var store = _sessionStore.Load();
            var conv = store.Conversations.FirstOrDefault(c => c.Id == _sessionId);
            if (conv != null)
            {
                Messages.Clear();
                foreach (var m in conv.Messages)
                    Messages.Add(m);
            }
        }
        catch (Exception ex) { KrnlLogger.Write($"LoadMessages: {ex.Message}"); }
    }

    private void PersistMessages()
    {
        if (!_persistenceEnabled || _sessionStore == null) return;
        try
        {
            var store = _sessionStore.Load();
            var existing = store.Conversations.FirstOrDefault(c => c.Id == _sessionId);
            if (existing != null)
                store.Conversations.Remove(existing);
            store.Conversations.Add(new ConversationData(2, _sessionId, _sessionId, [.. Messages], DateTime.UtcNow, DateTime.UtcNow));
            _sessionStore.Save(store with { ActiveConversationId = _sessionId });
        }
        catch (Exception ex) { KrnlLogger.Write($"PersistMessages: {ex.Message}"); }
    }

    private EmbeddedKrnlAI? GetOrCreateKernel()
    {
        if (_embeddedKernel != null) return _embeddedKernel;
        if (ServiceLocator.Instance.CurrentMode != RunMode.Local) return null;
        _embeddedKernel = ServiceLocator.Instance.EmbeddedKernel;
        return _embeddedKernel;
    }

    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) && _lastFrameJpeg == null) return;

        var text = InputText ?? "";
        SaveToHistory(text);

        // Check for slash command
        if (text.StartsWith("/"))
        {
            var cmdResult = await _slashHandler.ExecuteAsync(text);
            InputText = "";
            if (cmdResult == "CLEAR_CONVERSATION")
            {
                Messages.Clear();
                IsProcessing = false;
                return;
            }
            Messages.Add(new ChatMessage(
                Guid.NewGuid().ToString(),
                cmdResult,
                MessageRole.System,
                DateTime.Now,
                MessageStatus.Completed));
            IsProcessing = false;
            return;
        }

        IsProcessing = true;

        var hasImage = _lastFrameJpeg != null;
        var imageBase64 = hasImage ? Convert.ToBase64String(_lastFrameJpeg!) : null;

        var userMsg = new ChatMessage(
            Guid.NewGuid().ToString(),
            text,
            MessageRole.User,
            DateTime.Now,
            MessageStatus.Processing,
            ImageBase64: imageBase64);
        Messages.Add(userMsg);
        PersistMessages();

        InputText = "";

        if (hasImage)
        {
            StopCamera();
            DismissCameraPreview();
        }

        try
        {
            var imageBytes = _lastFrameJpeg;
            _lastFrameJpeg = null;

            if (GetOrCreateKernel() is { } kernel)
            {
                var result = await kernel.RunAsync(text ?? "");

                Messages.Add(new ChatMessage(
                    Guid.NewGuid().ToString(),
                    result.Narration ?? result.Error ?? "Sem resposta",
                    MessageRole.Assistant,
                    DateTime.Now,
                    string.IsNullOrEmpty(result.Error) ? MessageStatus.Completed : MessageStatus.Error));
            }
            else
            {
                var response = await _kernelClient.RunAgentAsync(new AgentRunRequest(
                    Prompt: text ?? "",
                    ImageBytes: imageBytes,
                    ImageFormat: imageBytes != null ? "jpeg" : null));

                var narration = response?.Narration;
                var error = response?.Error;

                Messages.Add(new ChatMessage(
                    Guid.NewGuid().ToString(),
                    narration ?? error ?? "Sem resposta",
                    MessageRole.Assistant,
                    DateTime.Now,
                    string.IsNullOrEmpty(error) ? MessageStatus.Completed : MessageStatus.Error));
                PersistMessages();

                if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(narration))
                {
                    try { _ = _kernelClient.SubmitFeedbackAsync(new FeedbackRequest("chat", 5, narration, "agent-response")); }
                    catch { }
                }

                if (!string.IsNullOrEmpty(narration))
                {
                    var audio = await _kernelClient.GenerateSpeechAsync(narration);
                    if (_isTtsEnabled && audio.Length > 0) await _audioPlayback.PlayAsync(audio);
                }
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage(Guid.NewGuid().ToString(), $"Erro: {ex.Message}", MessageRole.System, DateTime.Now, MessageStatus.Error));
        }
        finally
        {
            IsProcessing = false;
            _cognitiveStream.Disconnect();
        }
    }

    private void OnCognitiveEvent(CognitiveCycleEvent evt)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            // Update stages
            var stageIcons = new Dictionary<string, (string Icon, string Label)>
            {
                ["StepStarted"] = ("◔", "Running"),
                ["StepCompleted"] = ("●", "Done"),
                ["Error"] = ("!", "Error"),
                ["CycleCompleted"] = ("●", "Complete"),
            };

            if (stageIcons.TryGetValue(evt.Type, out var si))
            {
                var existing = CognitiveStages.FirstOrDefault(s => s.Label == evt.StepName);
                if (existing != null)
                {
                    existing.Icon = si.Icon;
                    existing.Detail = evt.Content ?? "";
                }
                else
                {
                    CognitiveStages.Add(new StageViewModel
                    {
                        Label = evt.StepName,
                        Detail = evt.Content ?? "",
                        Icon = si.Icon,
                        Background = Brushes.Transparent
                    });
                }
            }

            // Add to events
            CognitiveEvents.Add(new EventViewModel
            {
                Icon = evt.Type switch
                {
                    "StepStarted" => "▶", "StepCompleted" => "✅", "ToolCalled" => "🔧",
                    "Thought" => "💭", "SafetyCheck" => "🛡️", "Error" => "❌",
                    "CycleCompleted" => "🏁", _ => "•"
                },
                StepName = evt.StepName,
                Content = evt.Content
            });
            CognitiveDataChanged?.Invoke();
        });
    }

    private async Task ToggleAudioCaptureAsync()
    {
        if (IsCapturingAudio)
        {
            var audioData = await _audioCapture.StopCaptureAndGetAudioAsync();
            IsCapturingAudio = false;

            if (audioData.Length > 0 && _kernelClient != null)
            {
                var transcription = await _kernelClient.TranscribeAudioAsync(audioData);
                if (!string.IsNullOrEmpty(transcription))
                {
                    InputText = (InputText + " " + transcription).Trim();
                }
            }
        }
        else
        {
            await _audioCapture.StartCaptureAsync();
            IsCapturingAudio = true;
        }
    }

    private async Task ToggleCameraAsync()
    {
        if (IsCameraOn)
        {
            StopCamera();
            DismissCameraPreview();
        }
        else
        {
            var devices = _videoCapture.GetAvailableDevices();
            if (devices.Count > 0)
            {
                _videoCapture.FrameCaptured += OnFrameCaptured;
                await _videoCapture.StartCaptureAsync(devices[0].Id);
                IsCameraOn = true;
            }
        }
    }

    private void OnFrameCaptured(object? sender, VideoCaptureEventArgs e)
    {
        var bitmap = BitmapSource.Create(
            e.Width, e.Height, 96, 96,
            PixelFormats.Bgr24, null,
            e.ImageData, e.Width * 3);
        bitmap.Freeze();

        CameraPreviewSource = bitmap;

        using var ms = new MemoryStream();
        var encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(ms);
        _lastFrameJpeg = ms.ToArray();
    }

    private async Task SnapCameraAsync()
    {
        if (_lastFrameJpeg == null) return;
        StopCamera();
        await SendMessageAsync();
    }

    private void DismissCameraPreview()
    {
        CameraPreviewSource = null;
        _lastFrameJpeg = null;
    }

    private void StopCamera()
    {
        _videoCapture.FrameCaptured -= OnFrameCaptured;
        try { _ = _videoCapture.StopCaptureAsync(); }
        catch (Exception ex) { KrnlLogger.Write($"StopCamera: {ex.Message}"); }
        IsCameraOn = false;
    }

    public Task ConnectCognitiveStreamAsync()
    {
        return _cognitiveStream.ConnectAsync();
    }

    private void ApplySearchFilter()
    {
        FilteredMessages.Clear();
        if (string.IsNullOrWhiteSpace(_searchQuery))
            foreach (var m in Messages) FilteredMessages.Add(m);
        else
            foreach (var m in Messages.Where(m => m.Content?.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) == true))
                FilteredMessages.Add(m);
    }

    private void ShareConversation()
    {
        if (Messages.Count == 0) return;
        var text = string.Join("\n\n", Messages.Select(m => $"[{m.Timestamp:HH:mm}] {m.Role}: {m.Content}"));
        try { System.Windows.Clipboard.SetText(text); }
        catch (Exception ex) { KrnlLogger.Write($"Share: {ex.Message}"); }
    }

    private async Task EditMessageAsync(string messageId)
    {
        var msg = Messages.FirstOrDefault(m => m.Id == messageId);
        if (msg == null) return;
        var newContent = Microsoft.VisualBasic.Interaction.InputBox("Editar mensagem:", "Editar", msg.Content ?? "");
        if (!string.IsNullOrWhiteSpace(newContent) && newContent != msg.Content)
        {
            var idx = Messages.IndexOf(msg);
            Messages[idx] = msg with { Content = newContent };
            PersistMessages();
        }
        await Task.CompletedTask;
    }

    private void DeleteMessage(string messageId)
    {
        var msg = Messages.FirstOrDefault(m => m.Id == messageId);
        if (msg != null) { Messages.Remove(msg); PersistMessages(); }
    }

    private async Task ExportPdfAsync()
    {
        if (Messages.Count == 0) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "HTML files (*.html)|*.html",
            DefaultExt = ".html",
            FileName = $"chat-{DateTime.Now:yyyyMMdd-HHmmss}.html"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html><html><head><meta charset='UTF-8'><title>Chat Export</title>");
            html.AppendLine("<style>body{font-family:system-ui;max-width:800px;margin:auto;padding:20px}");
            html.AppendLine(".msg{margin:12px 0;padding:12px;border-radius:8px;background:#f5f5f5}");
            html.AppendLine(".user{background:#e3f2fd}.assistant{background:#f3e5f5}");
            html.AppendLine(".time{font-size:11px;color:#666}</style></head><body>");
            html.AppendLine($"<h1>Chat Export</h1><p>{DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            foreach (var m in Messages)
                html.AppendLine($"<div class='msg {m.Role.ToString().ToLower()}'><strong>{m.Role}:</strong> {System.Net.WebUtility.HtmlEncode(m.Content ?? "")}</div>");
            html.AppendLine("</body></html>");
            await File.WriteAllTextAsync(dialog.FileName, html.ToString());
        }
        catch (Exception ex) { KrnlLogger.Write($"ExportPdf: {ex.Message}"); }
    }

    private async Task ExportConversationAsync()
    {
        if (Messages.Count == 0) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = $"chat-export-{DateTime.Now:yyyyMMdd-HHmmss}.json"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var ext = Path.GetExtension(dialog.FileName).ToLower();
            if (ext == ".csv")
            {
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Timestamp,Role,Content,ImageBase64");
                foreach (var m in Messages)
                    csv.AppendLine($"\"{m.Timestamp:O}\",\"{m.Role}\",\"{m.Content?.Replace("\"", "\"\"")}\",\"{m.ImageBase64 ?? ""}\"");
                await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
            }
            else
            {
                var json = JsonSerializer.Serialize(Messages.ToList(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(dialog.FileName, json);
            }
        }
        catch (Exception ex) { KrnlLogger.Write($"Export: {ex.Message}"); }
    }

    public void Cleanup()
    {
        if (IsCameraOn) StopCamera();
        _cognitiveStream.OnEvent -= OnCognitiveEvent;
        _cognitiveStream.Disconnect();
        if (_embeddedKernel != null)
        {
            var kernel = _embeddedKernel;
            _embeddedKernel = null;
            _ = kernel.DisposeAsync().AsTask();
        }
    }
}
