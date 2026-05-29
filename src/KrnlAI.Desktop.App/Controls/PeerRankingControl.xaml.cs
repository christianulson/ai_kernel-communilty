using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.App.Controls;

public partial class PeerRankingControl : UserControl
{
    public PeerRankingControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            await vm.PeerRankingVM.LoadAsync();
    }

    private async void OnPeerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.PeerRankingVM.SelectedPeer is null)
            return;

        await vm.PeerRankingVM.LoadHistoryAsync(vm.PeerRankingVM.SelectedPeer.NodeId);
    }
}
