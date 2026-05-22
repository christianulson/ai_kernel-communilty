using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.App.Windows;

public partial class TrajectoryViewerWindow : Window
{
    public TrajectoryViewerWindow()
    {
        InitializeComponent();
        DataContext = new TrajectoryViewerViewModel();
    }

    private async void OnSessionDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SessionList.SelectedItem is TrajectorySessionSummary summary)
        {
            if (DataContext is TrajectoryViewerViewModel vm)
                await vm.LoadSessionAsync(summary.Id);
        }
    }
}
