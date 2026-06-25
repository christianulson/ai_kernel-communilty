using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public partial class ApiKeysControl : UserControl
{
    public ApiKeysControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is MainViewModel vm)
                await vm.ApiKeysVM.LoadAsync().ConfigureAwait(false);
        }
        catch (Exception ex) { KrnlLogger.Write($"ApiKeysControl.OnLoaded: {ex.Message}"); }
    }

    private void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || string.IsNullOrWhiteSpace(vm.ApiKeysVM.CreatedFullKey))
            return;

        Clipboard.SetText(vm.ApiKeysVM.CreatedFullKey);
        MessageBox.Show("Chave copiada para a área de transferência.", "API Keys", MessageBoxButton.OK, MessageBoxImage.Information);
        vm.ApiKeysVM.ClearCreatedKey();
    }

    private async void OnRevokeClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button button || DataContext is not MainViewModel vm || button.Tag is not string keyId)
                return;

            var confirm = MessageBox.Show(
                "Revogar esta API key? O acesso será encerrado imediatamente.",
                "Confirmar revogação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            await vm.ApiKeysVM.RevokeAsync(keyId).ConfigureAwait(false);
        }
        catch (Exception ex) { KrnlLogger.Write($"ApiKeysControl.OnRevokeClicked: {ex.Message}"); }
    }
}
