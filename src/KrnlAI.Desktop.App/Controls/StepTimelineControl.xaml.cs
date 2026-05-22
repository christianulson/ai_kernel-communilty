using System.Windows.Controls;
using System.Windows.Media;

namespace KrnlAI.Desktop.App.Controls;

public partial class StepTimelineControl : UserControl
{
    public StepTimelineControl()
    {
        InitializeComponent();
    }

    public void SetEvents(List<EventViewModel> events)
    {
        EventList.ItemsSource = events;
    }
}

public sealed class EventViewModel
{
    public string Icon { get; set; } = "";
    public string StepName { get; set; } = "";
    public string? Content { get; set; }
    public bool HasContent => !string.IsNullOrEmpty(Content);
    public string? Duration { get; set; }
    public Brush? Background { get; set; }
}
