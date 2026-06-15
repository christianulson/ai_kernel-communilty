using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class SearchOverlay : UserControl
{
    public event Action<string>? SearchRequested;
    public event Action? DismissRequested;

    public SearchOverlay()
    {
        InitializeComponent();
        Loaded += (_, _) => { SearchBox.Focus(); SearchBox.SelectAll(); };
    }

    public void SetResults(IReadOnlyList<string> results)
    {
        ResultsList.ItemsSource = results;
        ResultsText.Text = $"{results.Count} resultado(s)";
    }

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { DismissRequested?.Invoke(); e.Handled = true; }
        else if (e.Key == Key.Enter) { SearchRequested?.Invoke(SearchBox.Text); e.Handled = true; }
    }
}
