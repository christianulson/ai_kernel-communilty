using System.Windows;
using System.Windows.Controls;
using KrnlAI.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Dashboard;

public sealed record ModuleInfo(string Name, string Status);
public sealed record EventInfo(string Time, string Description);

public partial class DashboardControl : UserControl
{
    private readonly IDashboardService _service;
    private readonly IPoliciesService _policiesService;
    private readonly System.Threading.CancellationTokenSource _cts = new();

    public DashboardControl()
    {
        InitializeComponent();
        _service = new DashboardService();
        _policiesService = new PoliciesService();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _cts.Cancel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = RefreshAllAsync();
    }

    private async System.Threading.Tasks.Task RefreshAllAsync()
    {
        await System.Threading.Tasks.Task.WhenAll(
            RefreshScorecardAsync(),
            RefreshHealthAsync(),
            RefreshMoodAsync(),
            RefreshPoliciesAsync()
        );
        LoadSampleData();
    }

    private async System.Threading.Tasks.Task RefreshScorecardAsync()
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
    }

    private async System.Threading.Tasks.Task RefreshHealthAsync()
    {
        var health = await _service.GetHealthAsync(_cts.Token);
        if (health is not null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            KernelHealthText.Text = $"Kernel: {health.Status}" +
                (health.LatencyMs.HasValue ? $" ({health.LatencyMs:F0}ms)" : "");
            GatewayHealthText.Text = $"Gateway: {health.Status}" +
                (health.Version is not null ? $" | v{health.Version}" : "");
        }
    }

    private async System.Threading.Tasks.Task RefreshMoodAsync()
    {
        var mood = await _service.GetMoodAsync(_cts.Token);
        if (mood is not null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            MoodLabel.Text = mood;

            MoodIcon.Text = mood switch
            {
                string m when m.Contains("Excited") || m.Contains("Animado") => "⚡",
                string m when m.Contains("Calm") || m.Contains("Tranquilo") => "😌",
                string m when m.Contains("Tired") || m.Contains("Cansado") => "😮‍💨",
                string m when m.Contains("Tense") || m.Contains("Tenso") => "😰",
                string m when m.Contains("Focused") || m.Contains("Atento") => "🧐",
                _ => "😐"
            };
        }
    }

    private async System.Threading.Tasks.Task RefreshPoliciesAsync()
    {
        var policies = await _policiesService.GetPoliciesAsync(_cts.Token);
        if (policies.Count > 0)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            PoliciesList.ItemsSource = policies;
        }
    }

    private void LoadSampleData()
    {
        ModulesList.ItemsSource = new[]
        {
            new ModuleInfo("Cognition", "Running"),
            new ModuleInfo("Memory", "Running"),
            new ModuleInfo("Anticipation", "Standby"),
            new ModuleInfo("Executive", "Running"),
            new ModuleInfo("EventBus", "Running"),
        };

        EventsList.ItemsSource = new[]
        {
            new EventInfo("12:30", "Goal completed: Code review"),
            new EventInfo("12:28", "Memory consolidated"),
            new EventInfo("12:25", "Policy updated: Safety rule #4"),
            new EventInfo("12:20", "New episode started"),
            new EventInfo("12:15", "Mood shift detected"),
        };
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _ = RefreshAllAsync();
    }
}
