#pragma warning disable VSTHRD001 // Dispatcher is appropriate for WPF tool windows
#pragma warning disable VSTHRD100

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.ToolWindows;

public partial class DebugToolWindowControl : UserControl
{
    private VsOperationTracker? _tracker;
    private readonly System.Threading.CancellationTokenSource _cts = new();
    private readonly IBacklogService _backlog = new BacklogService();
    private const int MaxItems = 200;

    public DebugToolWindowControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _cts.Cancel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _tracker = VsGlobalTracker.Instance;
        if (_tracker is not null)
        {
            _tracker.OperationCompleted += OnOperationCompleted;
        }
        Unloaded += OnUnloaded;
        RefreshDisplay();
        _ = LoadLoopsAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_tracker is not null)
        {
            _tracker.OperationCompleted -= OnOperationCompleted;
        }
        _tracker = null;
        if (_backlog is IDisposable d) d.Dispose();
    }

    private void OnOperationCompleted(VsOperationCall op)
    {
        if (_cts.IsCancellationRequested) return;

        try
        {
            AddOperationItem(op);
        }
        catch (InvalidOperationException)
        {
            _ = Dispatcher.BeginInvoke(new Action(() => OnOperationCompleted(op)));
        }
    }

    private void AddOperationItem(VsOperationCall op)
    {
        var container = new Border
        {
            Margin = new Thickness(0, 1, 0, 1),
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(4),
            Background = GetStateColor(op.State),
        };

        var stack = new StackPanel();
        var header = new TextBlock
        {
            Text = $"[{op.StartedAt:HH:mm:ss.fff}] {op.Name}",
            FontWeight = FontWeights.SemiBold,
        };
        stack.Children.Add(header);

        var detail = new TextBlock
        {
            Text = FormatDetail(op),
            Foreground = new SolidColorBrush(Colors.Gray),
            FontSize = 11,
        };
        stack.Children.Add(detail);

        container.Child = stack;

        OperationList.Items.Add(container);

        // Trim old items
        while (OperationList.Items.Count > MaxItems)
            OperationList.Items.RemoveAt(0);

        CountText.Text = $"{_tracker?.History.Count ?? 0} operations";
    }
    private void RefreshDisplay()
    {
        OperationList.Items.Clear();
        if (_tracker is null) return;

        var history = _tracker.History;
        var ops = history.Count > 50
            ? history.Skip(history.Count - 50).ToList()
            : [.. history];

        foreach (var op in ops)
            AddOperationItem(op);

        CountText.Text = $"{_tracker.History.Count} operations";
        StatusText.Text = $"Showing {ops.Count} of {_tracker.History.Count} operations";
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (_tracker is null) return;
        var all = _tracker.History;
        var total = all.Count;
        var success = all.Count(o => o.State == VsOperationState.Completed);
        var failed = all.Count(o => o.State == VsOperationState.Failed);

        SummaryTotal.Text = $"Total: {total}";
        SummarySuccess.Text = $"✅ {success}";
        SummaryFailed.Text = $"❌ {failed}";

        // Average duration of completed operations
        var completed = all.Where(o => o.State == VsOperationState.Completed && o.ElapsedMs > 0).ToList();
        if (completed.Count > 0)
        {
            var avg = completed.Average(o => o.ElapsedMs);
            SummaryAvgDuration.Text = $"Ø {avg:F0}ms";

            // Top 3 slowest
            var slowest = completed.OrderByDescending(o => o.ElapsedMs).Take(3);
            var slowestText = string.Join(" | ", slowest.Select(o => $"{o.Name}: {o.ElapsedMs}ms"));
            SummarySlowest.Text = $"🐌 {slowestText}";
        }
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        _tracker?.Clear();
        OperationList.Items.Clear();
        CountText.Text = "0 operations";
        SummaryTotal.Text = "Total: 0";
        SummarySuccess.Text = "✅ 0";
        SummaryFailed.Text = "❌ 0";
        SummaryAvgDuration.Text = "Ø 0ms";
        SummarySlowest.Text = "";
        StatusText.Text = "Cleared";
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        RefreshDisplay();
        StatusText.Text = "Refreshed";
    }

    private async void OnLoopRefresh(object sender, RoutedEventArgs e)
    {
        await LoadLoopsAsync();
    }

    private async System.Threading.Tasks.Task LoadLoopsAsync()
    {
        LoopStatusText.Text = "Loading...";
        LoopRefreshButton.IsEnabled = false;

        try
        {
            var items = await _backlog.GetItemsAsync();
            if (items is null || items.Count == 0)
            {
                LoopList.ItemsSource = null;
                LoopCountText.Text = "0 items";
                LoopStatusText.Text = "No data";
                return;
            }

            var loopItems = items.Select(item => new
            {
                item.Id,
                item.Title,
                item.Status,
                StatusColor = GetLoopStatusBrush(item.Status),
                Steps = GenerateLoopSteps(item.Status)
            }).ToList();

            LoopList.ItemsSource = loopItems;
            LoopCountText.Text = $"{loopItems.Count} items";
            LoopStatusText.Text = $"Loaded ({loopItems.Count} items)";
        }
        catch (Exception ex)
        {
            LoopStatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            LoopRefreshButton.IsEnabled = true;
        }
    }

    private static List<object> GenerateLoopSteps(string status)
    {
        return new List<object>
        {
            new { Icon = status == "Pending" ? "⏳" : "✅", Label = "Pending", Detail = "(waiting)" },
            new { Icon = status == "InProgress" ? "🔄" : (IsPastOrCurrent("InProgress", status) ? "✅" : "⏳"), Label = "In Progress", Detail = IsPastOrCurrent("InProgress", status) ? "" : "(pending)" },
            new { Icon = status == "Review" ? "👁" : (IsPastOrCurrent("Review", status) ? "✅" : "⏳"), Label = "Review", Detail = IsPastOrCurrent("Review", status) ? "" : "(pending)" },
            new { Icon = status == "Done" ? "✅" : "⏳", Label = "Done", Detail = status == "Done" ? "(completed)" : "(pending)" },
        };
    }

    private static bool IsPastOrCurrent(string checkStatus, string currentStatus)
    {
        var order = new List<string> { "Pending", "InProgress", "Review", "Done", "Cancelled" };
        var checkIdx = order.IndexOf(checkStatus);
        var currentIdx = order.IndexOf(currentStatus);
        return currentIdx >= checkIdx;
    }

    private static SolidColorBrush GetLoopStatusBrush(string status)
    {
        return status switch
        {
            "Pending" => new SolidColorBrush(Color.FromRgb(102, 102, 102)),
            "InProgress" => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            "Review" => new SolidColorBrush(Color.FromRgb(255, 140, 0)),
            "Done" => new SolidColorBrush(Color.FromRgb(46, 160, 67)),
            "Cancelled" => new SolidColorBrush(Color.FromRgb(136, 136, 136)),
            _ => new SolidColorBrush(Color.FromRgb(102, 102, 102)),
        };
    }

    private static string FormatDetail(VsOperationCall op)
    {
        var parts = new List<string>();
        if (op.Arguments is not null)
            parts.Add($"args: {op.Arguments}");
        if (op.Result is not null)
            parts.Add($"→ {op.Result}");
        if (op.Error is not null)
            parts.Add($"✗ {op.Error}");

        var elapsed = op.ElapsedMs >= 1000
            ? $"{op.ElapsedMs / 1000.0:F1}s"
            : $"{op.ElapsedMs}ms";
        parts.Add(elapsed);

        return string.Join(" | ", parts);
    }

    private static SolidColorBrush GetStateColor(VsOperationState state)
    {
        return state switch
        {
            VsOperationState.Running => new SolidColorBrush(Color.FromArgb(20, 59, 130, 246)),
            VsOperationState.Completed => new SolidColorBrush(Color.FromArgb(12, 34, 197, 94)),
            VsOperationState.Failed => new SolidColorBrush(Color.FromArgb(20, 239, 68, 68)),
            VsOperationState.Cancelled => new SolidColorBrush(Color.FromArgb(12, 168, 85, 247)),
            _ => new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
        };
    }
}
