using System.Windows;
using System.Windows.Controls;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Services;

namespace KrnlAI.Desktop.App.Controls;

public partial class MultimodalControl : UserControl
{
    public MultimodalControl()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) { await Task.CompletedTask; }

    private async void OnIngest(object sender, RoutedEventArgs e)
    {
        var text = IngestInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            var client = ServiceLocator.Instance.KernelClient;
            await client.IngestMemoryAsync(new Core.Models.MemoryIngestRequest(text, "multimodal"));
            MessageBox.Show("Dados ingeridos com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            IngestInput.Clear();
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            MessageBox.Show($"Falha na ingestão: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnSearch(object sender, RoutedEventArgs e)
    {
        var query = SearchInput.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        try
        {
            StatusText.Text = "Buscando...";
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
            StatusText.Text = items is null || items.Count == 0 ? "Nenhum resultado." : $"{items.Count} resultados";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Falha na busca: {ex.Message}";
        }
    }
}
