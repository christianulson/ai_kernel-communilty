using System.Windows;
using System.Windows.Controls;

namespace KrnlAI.VisualStudio.ToolWindows.Backlog;

public sealed partial class BacklogCreateDialog : Window
{
    public string ItemTitle => TitleBox.Text.Trim();
    public string Description => DescBox.Text.Trim();
    public string Priority => PriorityCombo.SelectedItem is ComboBoxItem item ? item.Content?.ToString() ?? "Medium" : "Medium";

    public BacklogCreateDialog()
    {
        InitializeComponent();
    }

    private void OnCreate(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show("Title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
