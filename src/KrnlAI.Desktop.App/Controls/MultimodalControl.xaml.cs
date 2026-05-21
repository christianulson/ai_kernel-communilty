using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.App.Controls;

public partial class MultimodalControl : UserControl
{
    public MultimodalControl()
    {
        InitializeComponent();
    }

    private async void OnIngest(object sender, RoutedEventArgs e)
    {
        var text = IngestInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            var client = ServiceLocator.Instance.KernelClient;
            await client.IngestMemoryAsync(new Core.Models.MemoryIngestRequest(text, "multimodal"));
            MessageBox.Show("Data ingested successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            IngestInput.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ingest failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnSearch(object sender, RoutedEventArgs e)
    {
        var query = SearchInput.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        try
        {
            StatusText.Text = "Searching...";
            var client = ServiceLocator.Instance.KernelClient;
            var result = await client.SearchMultimodalAsync(query, 20);

            var items = result?.Hits?.Select(h => new
            {
                h.Id,
                h.Modality,
                h.Content,
                ScorePercent = $"{h.Score:P1}",
                SourceId = h.Id
            }).ToList();

            SearchResultsGrid.ItemsSource = items;
            StatusText.Text = items is null || items.Count == 0 ? "No results found." : $"{items.Count} results";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Search failed: {ex.Message}";
        }
    }
}
