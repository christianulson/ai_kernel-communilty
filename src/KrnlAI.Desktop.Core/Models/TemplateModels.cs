namespace KrnlAI.Desktop.Core.Models;

/// <summary>Information about a prompt template.</summary>
public sealed record TemplateInfo(
    string Id,
    string Name,
    string Description,
    string Content,
    string Category,
    string Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Request to create a new template.</summary>
public sealed record CreateTemplateRequest(
    string Name,
    string Description,
    string Content,
    string? Category = null);

/// <summary>Request to update an existing template.</summary>
public sealed record UpdateTemplateRequest(
    string? Name = null,
    string? Description = null,
    string? Content = null,
    string? Category = null);

/// <summary>Request to render a template with variables.</summary>
public sealed record RenderTemplateRequest(
    Dictionary<string, string> Variables);

/// <summary>Result of rendering a template.</summary>
public sealed record TemplateRenderResult(
    string? RenderedContent,
    string? Error);
