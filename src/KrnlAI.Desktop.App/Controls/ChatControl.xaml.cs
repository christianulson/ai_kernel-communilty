using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace KrnlAI.Desktop.App.Controls;
public partial class ChatControl : UserControl
{
    public ChatControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel mainVm && mainVm.ChatVM != null)
            {
                var chatVm = mainVm.ChatVM;

                await chatVm.ConnectCognitiveStreamAsync();
            }
        }
        catch (Exception ex) { KrnlAI.Desktop.Core.Services.KrnlLogger.Write($"ChatControl.OnLoaded: {ex.Message}"); }
    }

    private void OnChatInputKeyDown(object sender, KeyEventArgs e)
    {
        var mainVm = DataContext as ViewModels.MainViewModel;
        var chatVm = mainVm?.ChatVM;

        if (chatVm == null) return;

        // Mention suggestion navigation
        if (chatVm.IsMentionSuggestionsVisible)
        {
            if (e.Key == Key.Down)
            {
                chatVm.SelectNextMentionSuggestion();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Up)
            {
                chatVm.SelectPreviousMentionSuggestion();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                chatVm.ApplyMentionSuggestion();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape)
            {
                chatVm.IsMentionSuggestionsVisible = false;
                e.Handled = true;
                return;
            }
        }

        // Slash suggestion navigation
        if (chatVm.IsSlashSuggestionsVisible)
        {
            if (e.Key == Key.Down)
            {
                chatVm.SelectNextSlashSuggestion();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Up)
            {
                chatVm.SelectPreviousSlashSuggestion();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                chatVm.ApplySlashSuggestion();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape)
            {
                chatVm.IsSlashSuggestionsVisible = false;
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.None)
        {
            // Enter = send (unless slash suggestions visible)
            if (!chatVm.IsSlashSuggestionsVisible)
            {
                e.Handled = true;
                if (chatVm.SendMessageCommand.CanExecute(null))
                    chatVm.SendMessageCommand.Execute(null);
            }
            return;
        }
        if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
        {
            // Shift+Enter = new line (already handled by AcceptsReturn=True)
            return;
        }
        if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            // Ctrl+Enter = send (backward compat)
            e.Handled = true;
            if (chatVm.SendMessageCommand.CanExecute(null))
                chatVm.SendMessageCommand.Execute(null);
            return;
        }
        if (e.Key == Key.Up && e.KeyboardDevice.Modifiers == ModifierKeys.None)
        {
            // Up arrow = navigate message history (only when no slash suggestions)
            if (!chatVm.IsSlashSuggestionsVisible)
            {
                chatVm.NavigateHistoryUp();
                e.Handled = true;
            }
            return;
        }
        if (e.Key == Key.Down && e.KeyboardDevice.Modifiers == ModifierKeys.None)
        {
            // Down arrow = navigate message history
            if (!chatVm.IsSlashSuggestionsVisible)
            {
                chatVm.NavigateHistoryDown();
                e.Handled = true;
            }
            return;
        }
    }
}
