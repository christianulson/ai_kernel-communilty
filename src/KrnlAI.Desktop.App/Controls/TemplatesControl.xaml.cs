using System.Windows.Controls;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class TemplatesControl : UserControl
{
    public TemplatesControl() { InitializeComponent(); }
    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        try { if (DataContext is ViewModels.MainViewModel vm) vm.TemplatesVM.LoadTemplatesCommand.Execute(null); }
        catch (System.Exception ex) { KrnlLogger.Write($"TemplatesControl: {ex.Message}"); }
    }
}
