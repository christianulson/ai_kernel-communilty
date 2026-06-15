using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public partial class PeerRankingControl : UserControl
{
    public PeerRankingControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is MainViewModel vm)
                await vm.PeerRankingVM.LoadAsync();
        }
        catch (Exception ex) { KrnlLogger.Write($"PeerRankingControl.OnLoaded: {ex.Message}"); }
    }

    private async void OnPeerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (DataContext is not MainViewModel vm || vm.PeerRankingVM.SelectedPeer is null)
                return;
            await vm.PeerRankingVM.LoadHistoryAsync(vm.PeerRankingVM.SelectedPeer.NodeId);
        }
        catch (Exception ex) { KrnlLogger.Write($"PeerRankingControl.SelectionChanged: {ex.Message}"); }
    }
}
