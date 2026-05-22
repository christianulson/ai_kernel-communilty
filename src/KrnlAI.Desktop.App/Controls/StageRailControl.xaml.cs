using System.Windows.Controls;

namespace KrnlAI.Desktop.App.Controls;

public partial class StageRailControl : UserControl
{
    public StageRailControl()
    {
        InitializeComponent();
    }

    public void SetStages(List<StageViewModel> stages)
    {
        StageList.ItemsSource = stages;
    }
}

public sealed class StageViewModel
{
    public string Label { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Icon { get; set; } = "○";
    public System.Windows.Media.Brush? Background { get; set; }
    public System.Windows.Media.Brush? BorderBrush { get; set; }
}
