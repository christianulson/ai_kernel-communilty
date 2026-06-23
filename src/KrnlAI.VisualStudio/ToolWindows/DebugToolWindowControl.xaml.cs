#pragma warning disable VSTHRD001 // Dispatcher is appropriate for WPF tool windows

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KrnlAI.VisualStudio.Services;

namespace KrnlAI.VisualStudio.ToolWindows;

public partial class DebugToolWindowControl : UserControl
{
    private VsOperationTracker? _tracker;
    private readonly System.Threading.CancellationTokenSource _cts = new();
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
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_tracker is not null)
        {
            _tracker.OperationCompleted -= OnOperationCompleted;
        }
        _tracker = null;
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
