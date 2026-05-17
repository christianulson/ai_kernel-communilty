namespace Kernel.Contracts;

public enum ArtifactType { Mermaid, Chart, Table, Form, Code, Markdown }

public sealed record ArtifactDto(
    string Id,
    ArtifactType Type,
    string Title,
    string Content,
    string? Format,
    IReadOnlyDictionary<string, string>? Metadata,
    string? ParentArtifactId,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ExportRequest(
    string ArtifactId,
    string Format);

public sealed record ExportResult(
    string ArtifactId,
    string Format,
    byte[] Data,
    string ContentType);
