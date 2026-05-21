using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;
using SlashCommandInfo = KrnlAI.Desktop.App.Services.SlashCommandInfo;

namespace KrnlAI.Desktop.App.ViewModels;

public class ChatViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private readonly IAudioCapture _audioCapture;
    private readonly IAudioPlayback _audioPlayback;
    private readonly IVideoCapture _videoCapture;
    private readonly ILocalizationService _localization;

    // Message history for up/down navigation
    private readonly List<string> _messageHistory = new();
    private int _historyIndex = -1;
    private string _savedInput = string.Empty;

    // Slash commands
    private readonly SlashCommandService _slashCommands = new();
    private ObservableCollection<SlashCommandInfo> _slashSuggestions = new();
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

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    private string _inputText = "";
    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                ((AsyncRelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
                UpdateSlashSuggestions();
            }
        }
    }

    private void UpdateSlashSuggestions()
    {
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
    }

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

    public ChatViewModel(IKernelClient kernelClient, IAudioCapture audioCapture, IAudioPlayback audioPlayback, IVideoCapture videoCapture, ILocalizationService localization)
    {
        _kernelClient = kernelClient;
        _audioCapture = audioCapture;
        _audioPlayback = audioPlayback;
        _videoCapture = videoCapture;
        _localization = localization;
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, () => !IsProcessing && (!string.IsNullOrWhiteSpace(InputText) || _lastFrameJpeg != null));
        ClearChatCommand = new RelayCommand(() => Messages.Clear());
        ToggleAudioCaptureCommand = new AsyncRelayCommand(ToggleAudioCaptureAsync);
        ToggleCameraCommand = new AsyncRelayCommand(ToggleCameraAsync);
        SnapCameraCommand = new AsyncRelayCommand(SnapCameraAsync, () => _isCameraOn && _lastFrameJpeg != null);
        DismissCameraPreviewCommand = new RelayCommand(DismissCameraPreview);
    }

    public ChatViewModel() : this(
        ServiceLocator.Instance.KernelClient,
        ServiceLocator.Instance.AudioCapture,
        ServiceLocator.Instance.AudioPlayback,
        ServiceLocator.Instance.VideoCapture,
        ServiceLocator.Instance.LocalizationService)
    { }

    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) && _lastFrameJpeg == null) return;
        IsProcessing = true;

        var hasImage = _lastFrameJpeg != null;
        var imageBase64 = hasImage ? Convert.ToBase64String(_lastFrameJpeg!) : null;

        var userMsg = new ChatMessage(
            Guid.NewGuid().ToString(),
            InputText ?? "",
            MessageRole.User,
            DateTime.Now,
            MessageStatus.Processing,
            ImageBase64: imageBase64);
        Messages.Add(userMsg);

        var text = InputText ?? "";
        SaveToHistory(text);
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

            var response = await _kernelClient.RunAgentAsync(new AgentRunRequest(
                Prompt: text ?? "",
                ImageBytes: imageBytes,
                ImageFormat: imageBytes != null ? "jpeg" : null));

            Messages.Add(new ChatMessage(
                Guid.NewGuid().ToString(),
                response.Narration ?? response.Error ?? "Sem resposta",
                MessageRole.Assistant,
                DateTime.Now,
                string.IsNullOrEmpty(response.Error) ? MessageStatus.Completed : MessageStatus.Error));

            if (!string.IsNullOrEmpty(response.Narration))
            {
                var audio = await _kernelClient.GenerateSpeechAsync(response.Narration);
                if (audio.Length > 0) await _audioPlayback.PlayAsync(audio);
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage(Guid.NewGuid().ToString(), $"Erro: {ex.Message}", MessageRole.System, DateTime.Now, MessageStatus.Error));
        }
        finally { IsProcessing = false; }
    }

    private async Task ToggleAudioCaptureAsync()
    {
        if (IsCapturingAudio)
        {
            var audioData = await _audioCapture.StopCaptureAndGetAudioAsync();
            IsCapturingAudio = false;

            if (audioData.Length > 0)
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
        _ = _videoCapture.StopCaptureAsync();
        IsCameraOn = false;
    }

    public void Cleanup()
    {
        if (IsCameraOn) StopCamera();
    }
}
