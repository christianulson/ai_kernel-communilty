using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public partial class TrajectoryControl : UserControl
{
    public TrajectoryControl()
    {
        InitializeComponent();
    }

    private async void OnSessionDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            if (SessionList.SelectedItem is TrajectorySessionSummary summary)
            {
                if (DataContext is MainViewModel mainVm)
                    await mainVm.TrajectoryVM.LoadSessionAsync(summary.Id).ConfigureAwait(false);
            }
        }
        catch (Exception ex) { KrnlLogger.Write($"TrajectoryControl.OnSessionDoubleClick: {ex.Message}"); }
    }
}
