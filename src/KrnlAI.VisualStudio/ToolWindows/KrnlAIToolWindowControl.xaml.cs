using System.Windows;
using System.Windows.Controls;
using KrnlAI.VisualStudio.Commands.ChatCommands;
using KrnlAI.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows;

public partial class KrnlAIToolWindowControl : UserControl
{
    private readonly KernelClientService _clientService;
    private readonly ISolutionContextService _solutionContext;
    private readonly SlashCommandRouter _commandRouter;
    private readonly IApplyEditService _applyEdit;
    private readonly IAgenticLoopService _agenticLoop;
    private readonly System.Threading.CancellationTokenSource _cts = new();

    public KrnlAIToolWindowControl()
    {
        InitializeComponent();

        _clientService = new KernelClientService();
        _solutionContext = new SolutionContextService(ServiceProvider.GlobalProvider);
        _applyEdit = new ApplyEditService();
        _agenticLoop = new AgenticLoopService(_clientService);
        _commandRouter = new SlashCommandRouter(_clientService, _solutionContext, _applyEdit, _agenticLoop);

        _clientService.StateChanged += OnStateChanged;
        Loaded += OnLoaded;
        Unloaded += (_, _) => _cts.Cancel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = DoConnectAsync();
    }

    private void OnStateChanged(ConnectionState state)
    {
#pragma warning disable VSTHRD001, VSTHRD110 // Dispatcher.InvokeAsync is the correct pattern for event handlers from background threads
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
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            MoodText.Text = !string.IsNullOrEmpty(mood) ? mood : "";
            StatusMoodText.Text = !string.IsNullOrEmpty(mood) ? "Estado Emocional: " + mood : "";
        }
        catch { /* silent */ }
    }

    private async System.Threading.Tasks.Task DoConnectAsync()
    {
        ErrorText.Text = "";
        var settings = new SettingsService();
        settings.Load();

        var ok = await _clientService.ConnectAsync(settings.Endpoint, _cts.Token);
        if (!ok && !_cts.IsCancellationRequested)
        {
            ErrorText.Text = "Could not connect to Krnl-AI. Ensure the API is running.";
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

        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        ChatList.Items.Add(new ListBoxItem { Content = "You: " + text, IsEnabled = false });
        ChatInput.Clear();
        CommandSuggestions.Visibility = Visibility.Collapsed;

        if (_commandRouter.IsSlashCommand(text))
        {
            var result = await _commandRouter.ExecuteAsync(text, _cts.Token);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ChatList.Items.Add(new ListBoxItem { Content = "AI: " + result });
            return;
        }

        var codeContext = _solutionContext.GetActiveSelection();
        var fullPrompt = text;
        if (codeContext?.SelectedText is not null)
        {
            fullPrompt = text + "\n\n```" + (codeContext.Language ?? "") + "\n" + codeContext.SelectedText + "\n```";
            ChatList.Items.Add(new ListBoxItem
            {
                Content = "(using selection from " + System.IO.Path.GetFileName(codeContext.FilePath) + ")",
                IsEnabled = false,
            });
        }

        try
        {
            var result = await _clientService.RunAgentAsync(fullPrompt, ct: _cts.Token);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ChatList.Items.Add(new ListBoxItem { Content = "AI: " + (result.Summary ?? result.Status) });
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            ChatList.Items.Add(new ListBoxItem { Content = "Error: " + ex.Message, IsEnabled = false });
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
            var cmd = suggestion.Split(' ')[0]; // "/explain"
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
            foreach (var hit in result.Hits ?? System.Array.Empty<KrnlAI.Sdk.Models.MemorySearchHit>())
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
