using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.App.Controls;

public partial class TrajectoryControl : UserControl
{
    public TrajectoryControl()
    {
        InitializeComponent();
    }

    private async void OnSessionDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SessionList.SelectedItem is TrajectorySessionSummary summary)
        {
            if (DataContext is ViewModels.MainViewModel mainVm)
                await mainVm.TrajectoryVM.LoadSessionAsync(summary.Id);
        }
    }
}
