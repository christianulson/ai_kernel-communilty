using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace KrnlAI.VisualStudio.Services;

public sealed class SolutionContextService : ISolutionContextService
{
    private readonly IServiceProvider _serviceProvider;

    public SolutionContextService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public CodeSelection? GetActiveSelection()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            if (dte?.ActiveDocument is not Document doc) return null;

            var selection = doc.Selection as TextSelection;
            var textDocument = doc.Object("TextDocument") as EnvDTE.TextDocument;

            return new CodeSelection(
                FilePath: doc.FullName,
                FileExtension: System.IO.Path.GetExtension(doc.FullName) ?? "",
                Language: doc.Language,
                SelectedText: selection?.Text,
                SurroundingContext: textDocument?.StartPoint?.CreateEditPoint()
                    ?.GetText(textDocument.EndPoint),
                LineNumber: selection?.TopLine ?? 1,
                Column: selection?.CurrentColumn ?? 1
            );
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    public string? GetActiveFilePath()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return GetActiveSelection()?.FilePath;
    }

    public string? GetSolutionDirectory()
    {
        try
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            var solution = dte?.Solution;
            if (solution?.FullName is string path && !string.IsNullOrEmpty(path))
                return System.IO.Path.GetDirectoryName(path);
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
        }

        return null;
    }
}
