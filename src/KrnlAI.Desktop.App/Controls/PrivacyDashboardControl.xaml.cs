using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;
using KrnlAI.Desktop.Core.Models;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public partial class PrivacyDashboardControl : UserControl
{
    public PrivacyDashboardControl()
    {
        InitializeComponent();
        DataContext ??= new PrivacyDashboardViewModel();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is PrivacyDashboardViewModel vm)
            {
                await vm.LoadAsync().ConfigureAwait(false);
                SyncSelection(vm.SelectedConsentLevel);
            }
        }
        catch (Exception ex) { KrnlLogger.Write($"PrivacyDashboardControl.OnLoaded: {ex.Message}"); }
    }

    private void OnConsentChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radio || DataContext is not PrivacyDashboardViewModel vm)
            return;

        if (Enum.TryParse<TelemetryConsentLevel>(radio.Tag?.ToString(), out var level))
        {
            vm.SelectedConsentLevel = level;
            SyncSelection(level);
        }
    }

    private void SyncSelection(TelemetryConsentLevel level)
    {
        NoneConsentRadio.IsChecked = level == TelemetryConsentLevel.None;
        AnonymousConsentRadio.IsChecked = level == TelemetryConsentLevel.Anonymous;
        FullConsentRadio.IsChecked = level == TelemetryConsentLevel.Full;
    }
}
