#pragma warning disable VSTHRD100

using System.Windows;
using System.Windows.Controls;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.ToolWindows.Backlog;

public sealed partial class BacklogControl : UserControl, IDisposable
{
    private readonly IBacklogService _service;
    private IReadOnlyList<BacklogItem>? _items;
    private bool _disposed;

    private static readonly Dictionary<string, string> StatusOrder = new()
    {
        ["Pending"] = "InProgress",
        ["InProgress"] = "Review",
        ["Review"] = "Done",
        ["Done"] = "Cancelled",
        ["Cancelled"] = "Pending",
    };

    private static readonly Dictionary<string, string> PrevStatus = new()
    {
        ["Cancelled"] = "Done",
        ["Done"] = "Review",
        ["Review"] = "InProgress",
        ["InProgress"] = "Pending",
        ["Pending"] = "Cancelled",
    };

    public BacklogControl() : this(new BacklogService()) { }

    public BacklogControl(IBacklogService service)
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
            var statusFilter = StatusFilter.SelectedItem is ComboBoxItem si && si.Content?.ToString() is { } s && s != "All" ? s : null;
            var items = await _service.GetItemsAsync(statusFilter);
            _items = items;

            if (items is not null)
            {
                if (PriorityFilter.SelectedItem is ComboBoxItem pi && pi.Content?.ToString() is { } p && p != "All")
                    items = items.Where(i => i.Priority == p).ToList();

                ItemsGrid.ItemsSource = items;
                CountText.Text = $"{items.Count} items";
                StatusText.Text = $"Loaded ({items.Count} items)";
            }
            else
            {
                ItemsGrid.ItemsSource = null;
                CountText.Text = "0 items";
                StatusText.Text = "No data";
            }
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

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        _ = LoadDataAsync();
    }

    private void OnNewItem(object sender, RoutedEventArgs e)
    {
        var dialog = new BacklogCreateDialog();
        if (dialog.ShowDialog() == true)
        {
            _ = CreateItemAsync(dialog.ItemTitle, dialog.Description, dialog.Priority);
        }
    }

    private async System.Threading.Tasks.Task CreateItemAsync(string title, string description, string priority)
    {
        StatusText.Text = "Creating...";
        try
        {
            await _service.CreateItemAsync(title, description, priority, null);
            await LoadDataAsync();
            StatusText.Text = "Item created";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnMoveLeft(object sender, RoutedEventArgs e)
    {
        var item = GetSelectedItem();
        if (item is null) return;

        if (PrevStatus.TryGetValue(item.Status, out var prev))
        {
            StatusText.Text = $"Moving {item.Id} to {prev}...";
            var ok = await _service.MoveItemAsync(item.Id, prev);
            if (ok) await LoadDataAsync();
            else StatusText.Text = $"Failed to move {item.Id}";
        }
    }

    private async void OnMoveRight(object sender, RoutedEventArgs e)
    {
        var item = GetSelectedItem();
        if (item is null) return;

        if (StatusOrder.TryGetValue(item.Status, out var next))
        {
            StatusText.Text = $"Moving {item.Id} to {next}...";
            var ok = await _service.MoveItemAsync(item.Id, next);
            if (ok) await LoadDataAsync();
            else StatusText.Text = $"Failed to move {item.Id}";
        }
    }

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        var item = GetSelectedItem();
        if (item is null) return;

        var result = MessageBox.Show($"Delete \"{item.Title}\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        StatusText.Text = $"Deleting {item.Id}...";
        var ok = await _service.DeleteItemAsync(item.Id);
        if (ok) await LoadDataAsync();
        else StatusText.Text = $"Failed to delete {item.Id}";
    }

    private BacklogItem? GetSelectedItem()
    {
        if (ItemsGrid.SelectedItem is BacklogItem item)
        {
            SelectionText.Text = $"{item.Id}: {item.Title}";
            return item;
        }
        SelectionText.Text = "No item selected";
        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_service is IDisposable d) d.Dispose();
    }
}
