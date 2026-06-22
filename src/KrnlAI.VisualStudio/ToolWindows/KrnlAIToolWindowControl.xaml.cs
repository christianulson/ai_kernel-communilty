using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Services;
using KrnlAI.VisualStudio.ToolWindows.Chat.Artifacts;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows;

public partial class KrnlAIToolWindowControl : UserControl
{
    private readonly KernelClientService _clientService;
    private readonly ISolutionContextService _solutionContext;
    private readonly SlashCommandRouter _commandRouter;
    private readonly IApplyEditService _applyEdit;
    private readonly IAgenticLoopService _agenticLoop;
    private readonly ISignalRStreamingService _streamingService;
    private readonly IApprovalService _approvalService;
    private readonly ArtifactDispatcher _artifactDispatcher;
    private readonly ISettingsService _settings;
    private readonly System.Threading.CancellationTokenSource _cts = new();
    private CancellationTokenSource? _currentStreamCts;
    private string? _streamingBuffer;
    private Border? _streamingMessageBorder;
    private System.Windows.Threading.DispatcherTimer? _thinkingTimer;
    private int _thinkingDotCount;

    // Message history for up/down navigation
    private readonly System.Collections.Generic.List<string> _messageHistory = [];
    private int _historyIndex = -1;
    private string _savedInput = string.Empty;

    public KrnlAIToolWindowControl()
    {
        InitializeComponent();

        _settings = new SettingsService();
        _settings.Load();

        _clientService = new KernelClientService();
        _solutionContext = new SolutionContextService(ServiceProvider.GlobalProvider);
        _applyEdit = new ApplyEditService();
        _agenticLoop = new AgenticLoopService(_clientService);
        var debugTracker = new VsOperationTracker();
        _commandRouter = new SlashCommandRouter(_clientService, _solutionContext, _applyEdit, _agenticLoop, debugTracker: debugTracker);
        _streamingService = new SignalRStreamingService();
        _approvalService = new ApprovalService(_settings);
        _artifactDispatcher = new ArtifactDispatcher();

        _clientService.StateChanged += OnStateChanged;
        _streamingService.TokenReceived += OnStreamTokenReceived;
        _streamingService.StreamCompleted += OnStreamCompleted;
        _streamingService.ErrorReceived += OnStreamError;

        Loaded += OnLoaded;
        Unloaded += (_, _) => _cts.Cancel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = DoConnectAsync();
    }

    private void OnStateChanged(ConnectionState state)
    {
#pragma warning disable VSTHRD001, VSTHRD110
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => OnStateChanged(state));
            return;
        }
#pragma warning restore VSTHRD001, VSTHRD110

        StatusText.Text = state switch
        {
            ConnectionState.Disconnected => "Disconnected",
            ConnectionState.Connecting => "Connecting...",
            ConnectionState.Connected => "Connected",
            ConnectionState.Failed => "Failed",
            _ => "Unknown",
        };

        ConnectButton.IsEnabled = state is ConnectionState.Disconnected or ConnectionState.Failed;
        SendButton.IsEnabled = state == ConnectionState.Connected;
        SearchButton.IsEnabled = state == ConnectionState.Connected;

        if (state == ConnectionState.Connected)
            _ = UpdateMoodAsync();
    }

    private async System.Threading.Tasks.Task UpdateMoodAsync()
    {
        try
        {
            var mood = await _clientService.GetEmotionalMoodAsync(_cts.Token);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            MoodText.Text = !string.IsNullOrEmpty(mood) ? mood : "";
            StatusMoodText.Text = !string.IsNullOrEmpty(mood) ? "Estado Emocional: " + mood : "";
        }
        catch { }
    }

    private async System.Threading.Tasks.Task DoConnectAsync()
    {
        ErrorText.Text = "";
        _settings.Load();

        var endpoint = KernelEndpointResolver.Resolve(_settings.RuntimeMode, _settings.Endpoint, _settings.SidecarPort);
        var ok = await _clientService.ConnectAsync(endpoint, _cts.Token);
        if (!ok && !_cts.IsCancellationRequested)
        {
            ErrorText.Text = "Could not connect to Krnl-AI. Ensure the API is running.";
            return;
        }

        if (_settings.EnableStreaming && _clientService.BaseUrl != null)
        {
            try
            {
                var hubUrl = _clientService.BaseUrl.TrimEnd('/') + "/hubs/agent";
                await _streamingService.ConnectAsync(hubUrl, _cts.Token);
            }
            catch
            {
                // Streaming is optional; continue without it
            }
        }
    }

    private void OnConnect(object sender, RoutedEventArgs e)
    {
        _ = DoConnectAsync();
    }

    private async System.Threading.Tasks.Task DoSendAsync()
    {
        var text = ChatInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Save to message history
        if (!string.IsNullOrWhiteSpace(text))
        {
            if (_messageHistory.Count == 0 || _messageHistory[0] != text)
            {
                _messageHistory.Insert(0, text);
                if (_messageHistory.Count > 50) _messageHistory.RemoveAt(_messageHistory.Count - 1);
            }
            _historyIndex = -1;
        }

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        AddUserMessage(text);
        ChatInput.Clear();
        CommandSuggestions.Visibility = Visibility.Collapsed;

        if (_commandRouter.IsSlashCommand(text))
        {
            var result = await _commandRouter.ExecuteAsync(text, _cts.Token);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AddAiMessage(result);
            return;
        }

        var codeContext = _solutionContext.GetActiveSelection();
        var fullPrompt = text;
        if (codeContext?.SelectedText is not null)
        {
            fullPrompt = text + "\n\n```" + (codeContext.Language ?? "") + "\n" + codeContext.SelectedText + "\n```";
            AddSystemMessage("using selection from " + System.IO.Path.GetFileName(codeContext.FilePath));
        }

        try
        {
            ShowThinking(true);

            if (_settings.EnableStreaming && _streamingService.State == ConnectionState.Connected)
            {
                _currentStreamCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                _streamingBuffer = "";
                var streamingItem = AddStreamingMessage();

                await _streamingService.StartAgentStreamAsync(fullPrompt, Guid.NewGuid().ToString("N"), _currentStreamCts.Token);

                // Wait for stream to complete
                var tcs = new TaskCompletionSource<bool>();
                void OnCompleted() { tcs.TrySetResult(true); }
                _streamingService.StreamCompleted += OnCompleted;
                try
                {
                    await tcs.Task;
                }
                finally
                {
                    _streamingService.StreamCompleted -= OnCompleted;
                }

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                FinalizeStreamingMessage(streamingItem, _streamingBuffer ?? "");
            }
            else
            {
                var result = await _clientService.RunAgentAsync(fullPrompt, ct: _cts.Token);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var responseText = result.Summary ?? result.Status ?? "";
                AddArtifactMessage(responseText);
            }
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            AddErrorMessage(ex.Message);
        }
        finally
        {
            ShowThinking(false);
        }
    }

    private void OnStreamTokenReceived(string token)
    {
        _streamingBuffer += token;
#pragma warning disable VSTHRD001, VSTHRD110
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.InvokeAsync(() => AppendStreamToken(token));
            return;
        }
#pragma warning restore VSTHRD001, VSTHRD110
        AppendStreamToken(token);
    }

    private void AppendStreamToken(string token)
    {
        if (_streamingMessageBorder?.Child is TextBlock tb)
        {
            tb.Text += token;
            ChatScrollViewer?.ScrollToBottom();
        }
    }

    private void OnStreamCompleted()
    {
#pragma warning disable VSTHRD001, VSTHRD110
        Dispatcher.InvokeAsync(() =>
        {
            _streamingMessageBorder = null;
            ShowThinking(false);
        });
#pragma warning restore VSTHRD001, VSTHRD110
    }

    private void OnStreamError(string error)
    {
#pragma warning disable VSTHRD001, VSTHRD110
        Dispatcher.InvokeAsync(() =>
        {
            _streamingMessageBorder = null;
            AddErrorMessage(error);
            ShowThinking(false);
        });
#pragma warning restore VSTHRD001, VSTHRD110
    }

    private void OnStop(object sender, RoutedEventArgs e)
    {
        _currentStreamCts?.Cancel();
        ShowThinking(false);
    }

    private void ShowThinking(bool visible)
    {
        ThinkingIndicator.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        StreamingProgress.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        StopButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ChatInput.IsEnabled = !visible;
        SendButton.IsEnabled = !visible && _clientService.State == ConnectionState.Connected;

        if (visible)
        {
            _thinkingDotCount = 0;
            _thinkingTimer = new System.Windows.Threading.DispatcherTimer(
                TimeSpan.FromMilliseconds(500),
                System.Windows.Threading.DispatcherPriority.Normal,
                (_, _) =>
                {
                    _thinkingDotCount = (_thinkingDotCount + 1) % 4;
                    ThinkingIndicator.Text = "Agent thinking" + new string('.', _thinkingDotCount);
                },
                Dispatcher);
        }
        else
        {
            _thinkingTimer?.Stop();
            _thinkingTimer = null;
            ThinkingIndicator.Text = "Agent thinking...";
        }
    }

    private void AddUserMessage(string text)
    {
        ChatItems.Items.Add(new Border
        {
            Child = new TextBlock { Text = "You: " + text, TextWrapping = TextWrapping.Wrap },
            Margin = new Thickness(0, 2, 0, 2),
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Color.FromArgb(20, 56, 189, 248)),
            CornerRadius = new CornerRadius(8)
        });
    }

    private void AddAiMessage(string text)
    {
        ChatItems.Items.Add(new Border
        {
            Child = new TextBlock { Text = "AI: " + text, TextWrapping = TextWrapping.Wrap },
            Margin = new Thickness(0, 2, 0, 2),
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Color.FromArgb(12, 0, 0, 0)),
            CornerRadius = new CornerRadius(8)
        });
    }

    private void AddArtifactMessage(string text)
    {
        var container = new StackPanel
        {
            Margin = new Thickness(0, 2, 0, 2),
            Background = new SolidColorBrush(Color.FromArgb(12, 0, 0, 0))
        };

        var artifacts = _artifactDispatcher.RenderAll(text);
        foreach (var element in artifacts)
            container.Children.Add(element);

        ChatItems.Items.Add(new Border
        {
            Child = container,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 2, 0, 2)
        });
    }

    private Border AddStreamingMessage()
    {
        var border = new Border
        {
            Child = new TextBlock { Text = "AI: ", TextWrapping = TextWrapping.Wrap },
            Margin = new Thickness(0, 2, 0, 2),
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Color.FromArgb(12, 0, 0, 0)),
            CornerRadius = new CornerRadius(8)
        };
        ChatItems.Items.Add(border);
        _streamingMessageBorder = border;
        ChatScrollViewer?.ScrollToBottom();
        return border;
    }

    private void FinalizeStreamingMessage(Border border, string fullText)
    {
        if (_settings.EnableArtifactRendering)
        {
            var container = new StackPanel();
            var artifacts = _artifactDispatcher.RenderAll(fullText);
            foreach (var element in artifacts)
                container.Children.Add(element);
            border.Child = container;
        }
        else
        {
            ((TextBlock)border.Child).Text = "AI: " + fullText;
        }
        _streamingMessageBorder = null;
    }

    private void AddSystemMessage(string text)
    {
        ChatItems.Items.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush(Colors.Gray),
            Margin = new Thickness(0, 2, 0, 2)
        });
    }

    private void AddErrorMessage(string text)
    {
        ChatItems.Items.Add(new TextBlock
        {
            Text = "Error: " + text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Colors.Red),
            Margin = new Thickness(0, 2, 0, 2)
        });
    }

    private void OnChatInputKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Shift)
        {
            // Shift+Enter = new line (handled by AcceptsReturn=True)
            return;
        }
        if (e.Key == System.Windows.Input.Key.Enter && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            // Ctrl+Enter = send
            e.Handled = true;
            _ = DoSendAsync();
            return;
        }
        if (e.Key == System.Windows.Input.Key.Up && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.None)
        {
            if (_messageHistory.Count > 0 && _historyIndex < _messageHistory.Count - 1)
            {
                e.Handled = true;
                if (_historyIndex == -1) _savedInput = ChatInput.Text;
                _historyIndex++;
                ChatInput.Text = _messageHistory[_historyIndex];
                ChatInput.CaretIndex = ChatInput.Text.Length;
            }
            return;
        }
        if (e.Key == System.Windows.Input.Key.Down && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.None)
        {
            if (_historyIndex > -1)
            {
                e.Handled = true;
                _historyIndex--;
                ChatInput.Text = _historyIndex == -1 ? _savedInput : _messageHistory[_historyIndex];
                ChatInput.CaretIndex = ChatInput.Text.Length;
            }
            return;
        }
    }

    private void OnChatInputTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = ChatInput.Text;
        if (text.StartsWith("/") && text.Length > 1)
        {
            var partial = text.TrimStart('/').ToLowerInvariant();
            var matches = _commandRouter.Commands.Values
                .Where(c => c.Name.StartsWith(partial) && (c.IsVisible?.Invoke() ?? true))
                .Select(c => "/" + c.Name + " — " + c.Description)
                .ToList();

            if (matches.Count > 0)
            {
                CommandSuggestions.ItemsSource = matches;
                CommandSuggestions.Visibility = Visibility.Visible;
                return;
            }
        }
        CommandSuggestions.Visibility = Visibility.Collapsed;
    }

    private void OnCommandSuggestionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (CommandSuggestions.SelectedItem is string suggestion)
        {
            var cmd = suggestion.Split(' ')[0];
            ChatInput.Text = cmd + " ";
            ChatInput.CaretIndex = ChatInput.Text.Length;
            CommandSuggestions.Visibility = Visibility.Collapsed;
        }
    }

    private void OnSend(object sender, RoutedEventArgs e)
    {
        _ = DoSendAsync();
    }

    private async System.Threading.Tasks.Task DoSearchAsync()
    {
        var query = MemorySearchInput.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        MemoryList.Items.Clear();

        try
        {
            var result = await _clientService.SearchMemoryAsync(query, 10, _cts.Token);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            foreach (var hit in result.Hits ?? [])
            {
                var text = hit.Text.Length > 100 ? hit.Text.Substring(0, 100) : hit.Text;
                MemoryList.Items.Add(new ListBoxItem
                {
                    Content = string.Format("[{0:F2}] {1}", hit.Score, text),
                });
            }
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            MemoryList.Items.Add(new ListBoxItem { Content = "Error: " + ex.Message });
        }
    }

    private void OnSearch(object sender, RoutedEventArgs e)
    {
        _ = DoSearchAsync();
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _ = DoConnectAsync();
        _ = UpdateMoodAsync();
    }
}
