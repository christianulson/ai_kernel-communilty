using System.Windows;
using System.Windows.Controls;
using KrnlAI.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Dashboard;

public partial class DashboardControl : UserControl
{
    private readonly IDashboardService _service;
    private readonly System.Threading.CancellationTokenSource _cts = new();

    public DashboardControl()
    {
        InitializeComponent();
        _service = new DashboardService();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _cts.Cancel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = RefreshAsync();
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        var scorecard = await _service.GetScorecardAsync(_cts.Token);
        if (scorecard is not null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            GoalBar.Value = scorecard.GoalAutonomy * 100;
            ExecutionBar.Value = scorecard.ExecutionAutonomy * 100;
            SafetyBar.Value = scorecard.SafetyAutonomy * 100;
            LearningBar.Value = scorecard.LearningAutonomy * 100;
            MetaBar.Value = scorecard.MetaCognitionAutonomy * 100;
        }

        var health = await _service.GetHealthAsync(_cts.Token);
        if (health is not null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            HealthStatusText.Text = $"Status: {health.Status}";
            var details = health.LatencyMs.HasValue
                ? $"Latency: {health.LatencyMs:F0}ms"
                : "";
            if (health.Version is not null)
                details += $" | Version: {health.Version}";
            HealthDetailsText.Text = details;
        }

        var mood = await _service.GetMoodAsync(_cts.Token);
        if (mood is not null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            MoodText.Text = $"Emotional State: {mood}";
        }
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _ = RefreshAsync();
    }
}
