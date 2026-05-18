namespace KrnlAI.Desktop.Core.Models;

public record ModelRegistryEntry(string ModelId, string ModelVersion, string UseCase, string Runtime, string Status, string? ApprovedBy, DateTimeOffset CreatedAt, DateTimeOffset? ActivatedAt);

public record ModelRegistryDetail(string ModelId, IReadOnlyList<ModelRegistryEntry> Models, ModelRegistryEntry? Active);
