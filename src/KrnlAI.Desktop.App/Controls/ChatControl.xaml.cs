using System.Windows.Controls;
using System.Windows.Input;
namespace KrnlAI.Desktop.App.Controls;
public partial class ChatControl : UserControl
{
    public ChatControl() { InitializeComponent(); }

    private void OnChatInputKeyDown(object sender, KeyEventArgs e)
    {
        var mainVm = DataContext as ViewModels.MainViewModel;
        var chatVm = mainVm?.ChatVM;

        if (chatVm == null) return;

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

        if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
        {
            // Shift+Enter = new line (already handled by AcceptsReturn=True)
            return;
        }
        if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            // Ctrl+Enter = send
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
