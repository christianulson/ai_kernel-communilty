using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.App.ViewModels;

namespace KrnlAI.Desktop.App.Controls;

public partial class MindControl : UserControl
{
    public MindControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (DataContext is not MindViewModel viewModel)
        {
            viewModel = new MindViewModel(ServiceLocator.Instance.EmbeddedKernel);
            DataContext = viewModel;
        }

        await viewModel.LoadAsync();
    }
}
