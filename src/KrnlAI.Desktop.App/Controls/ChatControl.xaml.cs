using System.IO;
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
        Unloaded += (_, _) => { if (DataContext is ViewModels.MainViewModel mvm) mvm.ChatVM.CognitiveDataChanged -= OnCognitiveDataChanged; };
        AllowDrop = true;
        DragEnter += OnDragEnter;
        Drop += OnDrop;
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnImageClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.Image img && img.Source is System.Windows.Media.ImageSource src)
            {
                var window = new Window
                {
                    Title = "Preview", Width = 800, Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(8, 17, 31)),
                    WindowStyle = WindowStyle.SingleBorderWindow,
                    Topmost = true,
                    Content = new System.Windows.Controls.Image { Source = src, Stretch = System.Windows.Media.Stretch.Uniform, Margin = new Thickness(16) }
                };
                window.ShowDialog();
            }
        }
        catch (Exception ex) { KrnlAI.Desktop.Core.Services.KrnlLogger.Write($"ImagePreview: {ex.Message}"); }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        try
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                var content = await File.ReadAllTextAsync(files[0]);
                if (DataContext is ViewModels.MainViewModel mainVm && mainVm.ChatVM != null)
                {
                    mainVm.ChatVM.InputText = $"{mainVm.ChatVM.InputText}[file: {Path.GetFileName(files[0])}]\n{content[..Math.Min(content.Length, 2000)]}";
                }
            }
        }
        catch (Exception ex) { KrnlAI.Desktop.Core.Services.KrnlLogger.Write($"DragDrop: {ex.Message}"); }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is ViewModels.MainViewModel mainVm && mainVm.ChatVM != null)
            {
                var chatVm = mainVm.ChatVM;

                chatVm.CognitiveDataChanged += OnCognitiveDataChanged;
                await chatVm.ConnectCognitiveStreamAsync();
            }
        }
        catch (Exception ex) { KrnlAI.Desktop.Core.Services.KrnlLogger.Write($"ChatControl.OnLoaded: {ex.Message}"); }
    }

    private void OnCognitiveDataChanged()
    {
        if (DataContext is ViewModels.MainViewModel mainVm && mainVm.ChatVM != null)
        {
            StageRail.SetStages(mainVm.ChatVM.CognitiveStages);
            StepTimeline.SetEvents(mainVm.ChatVM.CognitiveEvents);
        }
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
