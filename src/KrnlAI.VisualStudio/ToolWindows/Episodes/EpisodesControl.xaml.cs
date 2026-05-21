using System.Windows;
using System.Windows.Controls;
using KrnlAI.VisualStudio.Services;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.ToolWindows.Episodes;

public partial class EpisodesControl : UserControl
{
    private readonly IEpisodesService _service;
    private readonly System.Threading.CancellationTokenSource _cts = new();

    public EpisodesControl()
    {
        InitializeComponent();
        _service = new EpisodesService();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _cts.Cancel();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = RefreshAsync();
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        try
        {
            var episodes = await _service.GetEpisodesAsync(_cts.Token);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            EpisodesList.ItemsSource = episodes;
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }

#pragma warning disable VSTHRD100
    private async void OnEpisodeSelected(object sender, SelectionChangedEventArgs e)
#pragma warning restore VSTHRD100
    {
        try
        {
            if (EpisodesList.SelectedItem is not Episode episode)
            {
                EpisodeDetails.Visibility = Visibility.Collapsed;
                return;
            }

            DetailsPanel.Children.Clear();

        if (episode.Steps is not null)
        {
            foreach (var step in episode.Steps)
            {
                var text = new TextBlock
                {
                    Text = $"#{step.Number} [{step.Tool}] {(step.Success ? "OK" : step.Result)}",
                    Margin = new Thickness(0, 0, 0, 4),
                };
                DetailsPanel.Children.Add(text);
            }
        }
        else
        {
            var details = await _service.GetEpisodeDetailsAsync(episode.Id, _cts.Token);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (details?.Steps is not null)
            {
                foreach (var step in details.Steps)
                {
                    var text = new TextBlock
                    {
                        Text = $"#{step.Number} [{step.Tool}] {(step.Success ? "OK" : step.Result)}",
                        Margin = new Thickness(0, 0, 0, 4),
                    };
                    DetailsPanel.Children.Add(text);
                }
            }
        }

        EpisodeDetails.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        _ = RefreshAsync();
    }
}
