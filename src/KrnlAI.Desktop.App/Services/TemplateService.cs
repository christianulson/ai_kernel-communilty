using System.IO;
using System.Text.Json;

namespace KrnlAI.Desktop.App.Services;

public sealed record PromptTemplate(string Id, string Name, string Description, string Content, DateTime CreatedAt);

public sealed class TemplateService
{
    private readonly string _filePath;
    private List<PromptTemplate> _templates = [];
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TemplateService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KrnlAI");
        _filePath = Path.Combine(dir, "templates.json");
        Load();
    }

    public IReadOnlyList<PromptTemplate> GetAll() => _templates.AsReadOnly();

    public void Save(string name, string description, string content)
    {
        _templates.Add(new PromptTemplate(Guid.NewGuid().ToString("N"), name, description, content, DateTime.UtcNow));
        Persist();
    }

    public void Delete(string id)
    {
        _templates.RemoveAll(t => t.Id == id);
        Persist();
    }

    public string GetContent(string id) => _templates.FirstOrDefault(t => t.Id == id)?.Content ?? "";

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _templates = JsonSerializer.Deserialize<List<PromptTemplate>>(json, JsonOpts) ?? [];
            }
        }
        catch { _templates = []; }
    }

    private void Persist()
    {
        try { File.WriteAllText(_filePath, JsonSerializer.Serialize(_templates, JsonOpts)); }
        catch { }
    }
}
