using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class ChatViewModel : ViewModelBase
{
    private readonly ServiceLocator _services;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    private string _inputText = "";
    public string InputText { get => _inputText; set { if (SetProperty(ref _inputText, value)) ((AsyncRelayCommand)SendMessageCommand).RaiseCanExecuteChanged(); } }
    private bool _isProcessing;
    public bool IsProcessing { get => _isProcessing; set { if (SetProperty(ref _isProcessing, value)) ((AsyncRelayCommand)SendMessageCommand).RaiseCanExecuteChanged(); } }

    // Audio capture
    private bool _isCapturingAudio;
    public bool IsCapturingAudio { get => _isCapturingAudio; set { SetProperty(ref _isCapturingAudio, value); OnPropertyChanged(nameof(AudioButtonIcon)); OnPropertyChanged(nameof(AudioButtonTooltip)); } }
    public string AudioButtonIcon => IsCapturingAudio ? "\U0001f534" : "\U0001f3a4";
    public string AudioButtonTooltip => IsCapturingAudio
        ? (_services?.LocalizationService.GetString("chat_audio_tooltip_stop") ?? "Stop recording")
        : (_services?.LocalizationService.GetString("chat_audio_tooltip_start") ?? "Record audio");

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

    public ChatViewModel()
    {
        _services = ServiceLocator.Instance;
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, () => !IsProcessing && (!string.IsNullOrWhiteSpace(InputText) || _lastFrameJpeg != null));
        ClearChatCommand = new RelayCommand(() => Messages.Clear());
        ToggleAudioCaptureCommand = new AsyncRelayCommand(ToggleAudioCaptureAsync);
        ToggleCameraCommand = new AsyncRelayCommand(ToggleCameraAsync);
        SnapCameraCommand = new AsyncRelayCommand(SnapCameraAsync, () => _isCameraOn && _lastFrameJpeg != null);
        DismissCameraPreviewCommand = new RelayCommand(DismissCameraPreview);
    }

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

        var text = InputText;
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

            var response = await _services.KernelClient.RunAgentAsync(new AgentRunRequest(
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
                var audio = await _services.KernelClient.GenerateSpeechAsync(response.Narration);
                if (audio.Length > 0) await _services.AudioPlayback.PlayAsync(audio);
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
            var audioData = await _services.AudioCapture.StopCaptureAndGetAudioAsync();
            IsCapturingAudio = false;

            if (audioData.Length > 0)
            {
                var transcription = await _services.KernelClient.TranscribeAudioAsync(audioData);
                if (!string.IsNullOrEmpty(transcription))
                {
                    InputText = (InputText + " " + transcription).Trim();
                }
            }
        }
        else
        {
            await _services.AudioCapture.StartCaptureAsync();
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
            var devices = _services.VideoCapture.GetAvailableDevices();
            if (devices.Count > 0)
            {
                _services.VideoCapture.FrameCaptured += OnFrameCaptured;
                await _services.VideoCapture.StartCaptureAsync(devices[0].Id);
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
        _services.VideoCapture.FrameCaptured -= OnFrameCaptured;
        _ = _services.VideoCapture.StopCaptureAsync();
        IsCameraOn = false;
    }

    public void Cleanup()
    {
        if (IsCameraOn) StopCamera();
    }
}
