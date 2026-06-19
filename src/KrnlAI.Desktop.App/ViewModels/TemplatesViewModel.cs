using System.Collections.ObjectModel;
using System.Windows.Input;
using KrnlAI.Desktop.App.Services;
using KrnlAI.Desktop.Core.Abstractions;
using KrnlAI.Desktop.Core.Models;

namespace KrnlAI.Desktop.App.ViewModels;

public class TemplatesViewModel : ViewModelBase
{
    private readonly IKernelClient _kernelClient;
    private string _newName = "", _newDescription = "", _newContent = "", _newCategory = "general", _errorMessage = "";
    private bool _isLoading;
    private string? _renderedContent;
    private string _selectedCategory = "Todos";
    private List<TemplateInfo> _allTemplates = [];

    public ObservableCollection<TemplateInfo> Templates { get; } = [];
    public ObservableCollection<TemplateInfo> FilteredTemplates { get; } = [];
    public ObservableCollection<string> Categories { get; } = ["Todos"];

    public string NewTemplateName { get => _newName; set => SetProperty(ref _newName, value); }
    public string NewTemplateDescription { get => _newDescription; set => SetProperty(ref _newDescription, value); }
    public string NewTemplateContent { get => _newContent; set => SetProperty(ref _newContent, value); }
    public string NewTemplateCategory { get => _newCategory; set => SetProperty(ref _newCategory, value); }
    public string ErrorMessage { get => _errorMessage; set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string? RenderedContent { get => _renderedContent; set => SetProperty(ref _renderedContent, value); }
    public string SelectedCategory { get => _selectedCategory; set { SetProperty(ref _selectedCategory, value); ApplyCategoryFilter(); } }

    public ICommand LoadTemplatesCommand { get; }
    public ICommand CreateTemplateCommand { get; }
    public ICommand DeleteTemplateCommand { get; }
    public ICommand RenderTemplateCommand { get; }
    public ICommand FilterByCategoryCommand { get; }
    public ICommand ClearErrorCommand { get; }
    public ICommand ClearRenderedContentCommand { get; }

    public TemplatesViewModel(IKernelClient kernelClient)
    {
        _kernelClient = kernelClient;
        LoadTemplatesCommand = new AsyncRelayCommand(LoadTemplatesAsync);
        CreateTemplateCommand = new AsyncRelayCommand(CreateTemplateAsync);
        DeleteTemplateCommand = new AsyncRelayCommand(p => DeleteTemplateAsync(p as string ?? ""));
        RenderTemplateCommand = new AsyncRelayCommand(p => RenderTemplateAsync(p as string ?? ""));
        FilterByCategoryCommand = new RelayCommand(p => SelectedCategory = p as string ?? "Todos");
        ClearErrorCommand = new RelayCommand(() => ErrorMessage = "");
        ClearRenderedContentCommand = new RelayCommand(() => RenderedContent = null);
    }

    public TemplatesViewModel() : this(ServiceLocator.Instance.KernelClient) { }

    public async Task LoadTemplatesAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            _allTemplates = await _kernelClient.TemplateListAsync();
            RefreshDisplay();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao carregar templates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task CreateTemplateAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTemplateName)) return;
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var request = new CreateTemplateRequest(NewTemplateName.Trim(), NewTemplateDescription.Trim(),
                NewTemplateContent.Trim(), string.IsNullOrWhiteSpace(NewTemplateCategory) ? null : NewTemplateCategory.Trim());
            await _kernelClient.TemplateCreateAsync(request);
            NewTemplateName = "";
            NewTemplateDescription = "";
            NewTemplateContent = "";
            await LoadTemplatesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao criar template: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteTemplateAsync(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId)) return;
        ErrorMessage = "";
        try
        {
            await _kernelClient.TemplateDeleteAsync(templateId);
            await LoadTemplatesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao excluir template: {ex.Message}";
        }
    }

    public async Task RenderTemplateAsync(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId)) return;
        ErrorMessage = "";
        RenderedContent = null;
        try
        {
            var request = new RenderTemplateRequest(new Dictionary<string, string>());
            var result = await _kernelClient.TemplateRenderAsync(templateId, request);
            if (result != null)
            {
                if (result.Error != null)
                    ErrorMessage = result.Error;
                else
                    RenderedContent = result.RenderedContent;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao renderizar template: {ex.Message}";
        }
    }

    public void ClearError() => ErrorMessage = "";
    public void ClearRenderedContent() => RenderedContent = null;

    private void ApplyCategoryFilter()
    {
        FilteredTemplates.Clear();
        var filtered = SelectedCategory == "Todos"
            ? _allTemplates
            : _allTemplates.Where(t => t.Category == SelectedCategory).ToList();
        foreach (var t in filtered) FilteredTemplates.Add(t);
    }

    private void RefreshDisplay()
    {
        Categories.Clear();
        Categories.Add("Todos");
        foreach (var cat in _allTemplates.Select(t => t.Category).Distinct().OrderBy(c => c))
            Categories.Add(cat);
        ApplyCategoryFilter();
    }
}
