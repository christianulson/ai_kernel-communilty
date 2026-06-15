using System.Collections.ObjectModel;
using System.Windows.Input;

namespace KrnlAI.Desktop.App.ViewModels;

public class PluginCatalogViewModel : ViewModelBase
{
    public ObservableCollection<string> Plugins { get; } = new()
    {
        "🔌 Local Filesystem - Acessar arquivos locais",
        "🔌 GitHub - Integração com repositórios",
        "🔌 SQL Database - Consultar bancos SQL",
        "🔌 Web Search - Pesquisar na web",
        "🔌 Slack - Mensagens no Slack",
        "🔌 Email - Enviar e receber emails"
    };
    public ICommand RefreshCommand { get; }
    public PluginCatalogViewModel() { RefreshCommand = new RelayCommand(() => { /* placeholder */ }); }
}
