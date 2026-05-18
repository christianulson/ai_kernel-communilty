namespace KrnlAI.Desktop.Core.Models;

public record ArchiveStats(bool Ok, int TotalArchived, IReadOnlyList<string> Stores);
