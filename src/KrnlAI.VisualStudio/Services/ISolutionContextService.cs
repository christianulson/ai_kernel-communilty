namespace KrnlAI.VisualStudio.Services;

public sealed record CodeSelection(
    string FilePath,
    string FileExtension,
    string? Language,
    string? SelectedText,
    string? SurroundingContext,
    int LineNumber,
    int Column
);

public interface ISolutionContextService
{
    CodeSelection? GetActiveSelection();
    string? GetActiveFilePath();
    string? GetSolutionDirectory();
}
