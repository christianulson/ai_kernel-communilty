using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace KrnlAI.VisualStudio.Services;

public sealed record EditorContext(
    string? FilePath,
    string? FileContent,
    string? SelectedText,
    int CaretLine,
    int CaretColumn,
    IReadOnlyList<EditorDiagnostic> Diagnostics
);

public sealed record EditorDiagnostic(
    string Message,
    string Severity,
    string? File,
    int Line,
    int Column
);

public interface IEditorContextProvider
{
    Task<EditorContext?> GetEditorContextAsync(CancellationToken ct = default);
}

public sealed class EditorContextProvider : IEditorContextProvider
{
    private readonly ITextBufferFactoryService _textBufferFactory;
    private readonly IContentTypeRegistryService _contentTypeRegistry;

    public EditorContextProvider()
    {
        _textBufferFactory = ServiceProvider.GlobalProvider.GetService(typeof(ITextBufferFactoryService)) as ITextBufferFactoryService
            ?? throw new InvalidOperationException("ITextBufferFactoryService not available");
        _contentTypeRegistry = ServiceProvider.GlobalProvider.GetService(typeof(IContentTypeRegistryService)) as IContentTypeRegistryService
            ?? throw new InvalidOperationException("IContentTypeRegistryService not available");
    }

    public EditorContextProvider(
        ITextBufferFactoryService textBufferFactory,
        IContentTypeRegistryService contentTypeRegistry)
    {
        _textBufferFactory = textBufferFactory;
        _contentTypeRegistry = contentTypeRegistry;
    }

    public async Task<EditorContext?> GetEditorContextAsync(CancellationToken ct = default)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

        try
        {
            var dte = ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            if (dte?.ActiveDocument is not EnvDTE.Document doc)
                return null;

            var filePath = doc.FullName;
            var selection = doc.Selection as EnvDTE.TextSelection;
            var textDoc = doc.Object("TextDocument") as EnvDTE.TextDocument;

            var fileContent = textDoc?.StartPoint?.CreateEditPoint()
                ?.GetText(textDoc.EndPoint);

            if (!System.IO.File.Exists(filePath))
                fileContent = null;

            var diagnostics = GetErrorListEntries(filePath);

            return new EditorContext(
                FilePath: filePath,
                FileContent: fileContent,
                SelectedText: selection?.Text,
                CaretLine: selection?.CurrentLine ?? 1,
                CaretColumn: selection?.CurrentColumn ?? 1,
                Diagnostics: diagnostics
            );
        }
        catch (Exception ex)
        {
            KrnlLogger.Write(ex);
            return null;
        }
    }

    private static IReadOnlyList<EditorDiagnostic> GetErrorListEntries(string? currentFile)
    {
        try
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var errorList = ServiceProvider.GlobalProvider.GetService(typeof(SVsErrorList)) as IVsTaskList;
            if (errorList is null) return Array.Empty<EditorDiagnostic>();

            errorList.EnumTaskItems(out var enumItems);
            if (enumItems is null) return Array.Empty<EditorDiagnostic>();

            var result = new List<EditorDiagnostic>();
            IVsTaskItem[] items = new IVsTaskItem[1];

            while (enumItems.Next(1, items, out var fetched) == 0 && fetched > 0)
            {
                var item = items[0];
                if (item is null) continue;

                item.Category(out var category);
                if (category != VSTASKCATEGORY.CAT_BUILD_COMPILE) continue;

                item.get_Text(out var message);
                item.get_FileName(out var file);
                item.get_Line(out var line);
                item.get_Column(out var column);
                item.Priority(out var priority);

                if (string.IsNullOrEmpty(message)) continue;

                var severity = priority switch
                {
                    VSTASKPRIORITY.TP_HIGH => "Error",
                    VSTASKPRIORITY.TP_NORMAL => "Warning",
                    VSTASKPRIORITY.TP_LOW => "Info",
                    _ => "Unknown"
                };

                result.Add(new EditorDiagnostic(
                    message!,
                    severity,
                    file,
                    line,
                    column
                ));
            }

            return result;
        }
        catch
        {
            return Array.Empty<EditorDiagnostic>();
        }
    }
}
