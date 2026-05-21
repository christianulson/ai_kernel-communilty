using System.Windows.Controls;

namespace KrnlAI.Desktop.App.Controls;

public partial class AdminConfigControl : UserControl
{
    public AdminConfigControl()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadFeatures();
    }

    private void LoadFeatures()
    {
        FeaturesGrid.ItemsSource = new List<FeatureFlagItem>
        {
            new("Streaming", true, "Chat"),
            new("Artifact Rendering", true, "Chat"),
            new("Inline Completions", true, "Editor"),
            new("CodeLens", true, "Editor"),
            new("Usage Tracking", true, "Privacy"),
        };
    }

    private sealed record FeatureFlagItem(string Name, bool Enabled, string Category);
}
