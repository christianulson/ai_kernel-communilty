namespace KrnlAI.Cli.Abstractions;

public interface ITemplateEngine
{
    Task ScaffoldAsync(TemplateType type, string name, string outputDir, IReadOnlyDictionary<string, string>? variables = null);
    Task<IReadOnlyList<TemplateInfo>> ListTemplatesAsync();
}

public enum TemplateType
{
    Agent,
    CognitiveCycle,
    Tool,
    Policy
}

public sealed record TemplateInfo(
    string Name,
    string Description,
    TemplateType Type,
    string Version,
    string[] Dependencies);
