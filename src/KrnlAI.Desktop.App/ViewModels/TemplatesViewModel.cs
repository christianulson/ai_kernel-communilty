using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;

namespace KrnlAI.Desktop.App.ViewModels;

public class TemplatesViewModel : ViewModelBase
{
    private readonly TemplateService _service = new();
    private string _name = "", _description = "", _content = "";
    public ObservableCollection<PromptTemplate> Templates { get; } = [];
    public string TemplateName { get => _name; set => SetProperty(ref _name, value); }
    public string TemplateDescription { get => _description; set => SetProperty(ref _description, value); }
    public string TemplateContent { get => _content; set => SetProperty(ref _content, value); }
    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand InsertCommand { get; }

    public TemplatesViewModel()
    {
        LoadCommand = new RelayCommand(() => { Templates.Clear(); foreach (var t in _service.GetAll()) Templates.Add(t); });
        SaveCommand = new AsyncRelayCommand(async () =>
        {
            if (string.IsNullOrWhiteSpace(_name)) return;
            _service.Save(_name, _description, _content);
            TemplateName = ""; TemplateDescription = ""; TemplateContent = "";
            LoadCommand.Execute(null);
            await Task.CompletedTask;
        });
        DeleteCommand = new AsyncRelayCommand(async p => { if (p is string id) { _service.Delete(id); LoadCommand.Execute(null); } });
        InsertCommand = new AsyncRelayCommand(async p => { if (p is string id) TryInsertTemplate(id); await Task.CompletedTask; });
    }

    private void TryInsertTemplate(string id)
    {
        var content = _service.GetContent(id);
        if (string.IsNullOrEmpty(content)) return;
        var app = System.Windows.Application.Current;
        if (app?.MainWindow?.DataContext is MainViewModel vm)
            vm.ChatVM.InputText = content;
    }
}
