using System.Windows;
using System.Windows.Controls;
using KrnlAI.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Policies;

public partial class PoliciesControl : UserControl
{
    private readonly IPoliciesService _service;
    private readonly System.Threading.CancellationTokenSource _cts = new();

    public PoliciesControl()
    {
        InitializeComponent();
        _service = new PoliciesService();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _cts.Cancel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = RefreshAsync();
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        try
        {
            var policies = await _service.GetPoliciesAsync(_cts.Token);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var filter = (DomainFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var filtered = filter is null || filter == "All"
                ? policies
                : policies.Where(p => string.Equals(p.Domain, filter, System.StringComparison.OrdinalIgnoreCase)).ToList();

            PoliciesList.ItemsSource = filtered;
        }
        catch { }
    }

    private void OnDomainFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        _ = RefreshAsync();
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _ = RefreshAsync();
    }
}
