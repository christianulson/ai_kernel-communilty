using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.App.Controls;

public sealed partial class WelcomeWizardControl : UserControl
{
    public WelcomeWizardControl()
    {
        InitializeComponent();
    }

    public void Show()
    {
        Visibility = Visibility.Visible;
        if (DataContext is WelcomeWizardViewModel vm)
            vm.Reset();
    }
}
