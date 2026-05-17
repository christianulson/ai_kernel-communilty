using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIKernel.Cli.Abstractions;
using Microsoft.Extensions.Logging;

namespace AIKernel.Cli.Services;

public sealed partial class TemplateEngine : ITemplateEngine
{
    private readonly ILogger<TemplateEngine> _logger;
    private static readonly string TemplatesRoot = Path.Combine(AppContext.BaseDirectory, "Templates");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly ConcurrentDictionary<string, TemplateManifest> ManifestCache = new();
    private static readonly Regex PlaceholderPattern = PlaceholderRegex();

    public TemplateEngine(ILogger<TemplateEngine>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TemplateEngine>.Instance;
    }

    public Task<IReadOnlyList<TemplateInfo>> ListTemplatesAsync()
    {
        var manifest = LoadManifest();
        var templates = manifest?.Templates
            .Select(t => new TemplateInfo(
                t.Name, t.Description,
                Enum.Parse<TemplateType>(t.Type),
                t.Version, t.Dependencies))
            .ToList() ?? new List<TemplateInfo>();

        return Task.FromResult<IReadOnlyList<TemplateInfo>>(templates);
    }

    private string GetTemplateDirectory(TemplateType type, string templateName = "default")
    {
        var typeDir = type switch
        {
            TemplateType.Agent => "Agent",
            TemplateType.Tool => "Tool",
            TemplateType.Policy => "Policy",
            TemplateType.CognitiveCycle => "CognitiveCycle",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        if (type == TemplateType.Agent && !string.IsNullOrEmpty(templateName) && templateName != "default")
        {
            var namedDir = Path.Combine(TemplatesRoot, typeDir, templateName);
            if (Directory.Exists(namedDir))
                return namedDir;
        }

        var defaultDir = Path.Combine(TemplatesRoot, typeDir, "Basic");
        if (Directory.Exists(defaultDir))
            return defaultDir;

        return Path.Combine(TemplatesRoot, typeDir);
    }

    public async Task ScaffoldAsync(TemplateType type, string name, string outputDir, IReadOnlyDictionary<string, string>? variables = null)
    {
        var templateName = variables?.TryGetValue("TemplateName", out var tn) == true ? tn : "default";
        var templateDir = GetTemplateDirectory(type, templateName);
        if (!Directory.Exists(templateDir))
            throw new DirectoryNotFoundException($"Template directory not found: {templateDir} (type={type}, template={templateName})");

        var vars = BuildVariables(type, name, variables);
        EnsureDirectoryExists(outputDir);

        await CopyTemplateRecursiveAsync(templateDir, outputDir, vars);

        _logger.LogInformation("Scaffolded {Type} '{Name}' at {Output}", type, name, outputDir);
    }

    private static Dictionary<string, string> BuildVariables(TemplateType type, string name, IReadOnlyDictionary<string, string>? custom)
    {
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AgentName"] = name,
            ["AgentNamespace"] = SanitizeNamespace(name),
            ["ToolName"] = name,
            ["ToolId"] = SanitizeToolId(name),
            ["ToolDescription"] = $"Custom tool: {name}",
            ["ToolNamespace"] = SanitizeNamespace(name),
            ["PolicyName"] = name,
            ["PolicyId"] = SanitizeToolId(name),
            ["PolicyDescription"] = $"Custom policy: {name}",
            ["PolicyNamespace"] = SanitizeNamespace(name),
            ["CycleNamespace"] = SanitizeNamespace(name),
            ["SafetyLevel"] = "strict",
            ["AgentGoal"] = "Accomplish tasks autonomously",
            ["LlmProvider"] = "ollama",
            ["LlmModel"] = "llama3.2",
            ["MemoryEnabled"] = "true"
        };

        if (custom is not null)
        {
            foreach (var (key, value) in custom)
                vars[key] = value;
        }

        return vars;
    }

    private static async Task CopyTemplateRecursiveAsync(string sourceDir, string destDir, Dictionary<string, string> vars)
    {
        var createdDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var fileName = Path.GetFileName(relativePath);

            if (fileName.EndsWith(".template", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = Path.Combine(
                    Path.GetDirectoryName(relativePath) ?? "",
                    Path.GetFileNameWithoutExtension(fileName));
            }

            relativePath = ReplacePlaceholders(relativePath, vars);
            var destPath = Path.Combine(destDir, relativePath);

            var parentDir = Path.GetDirectoryName(destPath)!;
            if (createdDirs.Add(parentDir))
                EnsureDirectoryExists(parentDir);

            var content = await File.ReadAllTextAsync(file);
            content = ReplacePlaceholders(content, vars);
            await File.WriteAllTextAsync(destPath, content);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, subDir);
            relativePath = ReplacePlaceholders(relativePath, vars);
            var dirPath = Path.Combine(destDir, relativePath);
            if (createdDirs.Add(dirPath))
                EnsureDirectoryExists(dirPath);
        }
    }

    private static string ReplacePlaceholders(string input, Dictionary<string, string> vars)
    {
        return PlaceholderPattern.Replace(input, match =>
        {
            var key = match.Groups[1].Value;
            return vars.GetValueOrDefault(key, match.Value);
        });
    }

    private static string SanitizeNamespace(string name)
    {
        var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_.]", "_");
        if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            sanitized = "_" + sanitized;
        return sanitized;
    }

    private static string SanitizeToolId(string name)
    {
        var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_-]", "_");
        return sanitized.Trim('-').ToLowerInvariant();
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    private TemplateManifest? LoadManifest()
    {
        var manifestPath = Path.Combine(TemplatesRoot, "templates.json");
        if (!File.Exists(manifestPath))
            return null;

        return ManifestCache.GetOrAdd("manifest", _ =>
        {
            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<TemplateManifest>(json, JsonOptions)
                   ?? new TemplateManifest { Templates = [] };
        });
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    private sealed record TemplateManifest
    {
        public List<TemplateEntry> Templates { get; init; } = [];
    }

    private sealed record TemplateEntry
    {
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string Type { get; init; } = "";
        public string Version { get; init; } = "";
        public string[] Dependencies { get; init; } = [];
    }
}
