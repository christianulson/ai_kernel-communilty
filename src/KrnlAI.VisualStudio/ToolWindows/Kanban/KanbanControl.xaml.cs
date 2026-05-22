using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.ToolWindows.Kanban;

public sealed partial class KanbanControl : UserControl, IDisposable
{
    private readonly IKanbanService _service;
    private bool _disposed;

    public KanbanControl() : this(new KanbanService()) { }

    public KanbanControl(IKanbanService service)
    {
        InitializeComponent();
        _service = service;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = LoadDataAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }

    private async System.Threading.Tasks.Task LoadDataAsync()
    {
        StatusText.Text = "Loading...";
        RefreshButton.IsEnabled = false;

        try
        {
            var days = DaysCombo.SelectedItem is ComboBoxItem item
                && int.TryParse(item.Content?.ToString(), out var d) ? d : 10;
            var domain = string.IsNullOrWhiteSpace(DomainBox.Text) ? null : DomainBox.Text.Trim();

            var data = await _service.GetKanbanAsync(days, domain);
            ColumnsControl.ItemsSource = data?.Columns ?? [];
            StatusText.Text = data is not null ? $"Loaded ({data.Metadata.TotalGoals} goals)" : "No data";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _ = LoadDataAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_service is IDisposable d) d.Dispose();
    }
}
