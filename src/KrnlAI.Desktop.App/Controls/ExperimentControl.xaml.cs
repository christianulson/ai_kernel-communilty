using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class ExperimentControl : UserControl
{
    public ExperimentControl() { InitializeComponent(); }
    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try { if (DataContext is ViewModels.MainViewModel vm) vm.ExperimentsVM.LoadExperimentsCommand.Execute(null); }
        catch (System.Exception ex) { KrnlLogger.Write($"ExperimentControl: {ex.Message}"); }
    }
}
