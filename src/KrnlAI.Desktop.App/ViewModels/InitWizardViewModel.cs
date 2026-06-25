using System.IO;
using System.Windows.Input;

namespace KrnlAI.Desktop.App.ViewModels;

public class InitWizardViewModel : ViewModelBase
{
    private string _projectName = "MyKrnlAIProject";
    private string _location = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private string _template = "agent";
    private string _status = "Pronto.";
    public string ProjectName { get => _projectName; set => SetProperty(ref _projectName, value); }
    public string Location { get => _location; set => SetProperty(ref _location, value); }
    public string SelectedTemplate { get => _template; set => SetProperty(ref _template, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string[] Templates { get; } = ["agent", "tool", "policy", "cognitive-cycle"];
    public ICommand BrowseCommand { get; }
    public ICommand CreateCommand { get; }

    public InitWizardViewModel()
    {
        BrowseCommand = new RelayCommand(() =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { ValidateNames = false, CheckFileExists = false, CheckPathExists = true, FileName = "Selecionar Pasta" };
            if (dlg.ShowDialog() == true) Location = Path.GetDirectoryName(dlg.FileName) ?? _location;
        });
        CreateCommand = new AsyncRelayCommand(async () =>
        {
            Status = "Criando projeto...";
            var targetDir = Path.Combine(_location, _projectName);
            try
            {
                Directory.CreateDirectory(targetDir);
                await File.WriteAllTextAsync(Path.Combine(targetDir, "README.md"), $"# {_projectName}\n\nKrnlAI {_template} project created at {DateTime.Now:g}.\n").ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(targetDir, "config.yaml"), $"mode: {_template}\nname: {_projectName}\n").ConfigureAwait(false);
                Status = $"Projeto criado em: {targetDir}";
            }
            catch (Exception ex) { Status = $"Erro: {ex.Message}"; }
        });
    }
}
