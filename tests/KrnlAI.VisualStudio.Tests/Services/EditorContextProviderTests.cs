using KrnlAI.VisualStudio.Services;
using Xunit;

namespace KrnlAI.VisualStudio.Tests.Services;

[Trait("Category", "Unit")]
public sealed class EditorContextProviderTests
{
    [Fact]
    public void EditorContext_Constructor_ShouldSetProperties()
    {
        var diagnostics = new List<EditorDiagnostic>
        {
            new("CS1001", "Error", "test.cs", 10, 5)
        };
        var context = new EditorContext(
            FilePath: @"C:\test\file.cs",
            FileContent: "class Foo { }",
            SelectedText: "Foo",
            CaretLine: 1,
            CaretColumn: 7,
            Diagnostics: diagnostics
        );

        Assert.Equal(@"C:\test\file.cs", context.FilePath);
        Assert.Equal("class Foo { }", context.FileContent);
        Assert.Equal("Foo", context.SelectedText);
        Assert.Equal(1, context.CaretLine);
        Assert.Equal(7, context.CaretColumn);
        Assert.Single(context.Diagnostics);
    }

    [Fact]
    public void EditorContext_NullFilePath_ShouldBeAllowed()
    {
        var context = new EditorContext(
            FilePath: null,
            FileContent: null,
            SelectedText: null,
            CaretLine: 0,
            CaretColumn: 0,
            Diagnostics: []
        );

        Assert.Null(context.FilePath);
        Assert.Null(context.FileContent);
        Assert.Empty(context.Diagnostics);
    }

    [Fact]
    public void EditorDiagnostic_Constructor_ShouldSetProperties()
    {
        var diag = new EditorDiagnostic(
            Message: "CS1001: Identifier expected",
            Severity: "Error",
            File: @"C:\test\file.cs",
            Line: 15,
            Column: 3
        );

        Assert.Equal("CS1001: Identifier expected", diag.Message);
        Assert.Equal("Error", diag.Severity);
        Assert.Equal(@"C:\test\file.cs", diag.File);
        Assert.Equal(15, diag.Line);
        Assert.Equal(3, diag.Column);
    }

    [Fact]
    public void EditorDiagnostic_EmptyMessage_ShouldBeAllowed()
    {
        var diag = new EditorDiagnostic(
            Message: string.Empty,
            Severity: "Warning",
            File: null,
            Line: 0,
            Column: 0
        );

        Assert.Empty(diag.Message);
        Assert.Equal("Warning", diag.Severity);
    }

    [Fact]
    public void EditorContext_WithMultipleDiagnostics_ShouldStoreAll()
    {
        var diagnostics = new List<EditorDiagnostic>
        {
            new("Error 1", "Error", "f1.cs", 1, 1),
            new("Error 2", "Warning", "f2.cs", 2, 2),
            new("Error 3", "Info", "f3.cs", 3, 3),
        };

        var context = new EditorContext(
            FilePath: "test.cs",
            FileContent: "",
            SelectedText: "",
            CaretLine: 0,
            CaretColumn: 0,
            Diagnostics: diagnostics
        );

        Assert.Equal(3, context.Diagnostics.Count);
    }

    [Fact]
    public void EditorContext_Immutability_ShouldNotAllowModification()
    {
        var diag = new EditorDiagnostic("msg", "Error", null, 0, 0);
        var diagnostics = new List<EditorDiagnostic> { diag }.AsReadOnly();

        var context = new EditorContext(
            FilePath: "test.cs",
            FileContent: "content",
            SelectedText: "selected",
            CaretLine: 5,
            CaretColumn: 10,
            Diagnostics: diagnostics
        );

        Assert.Equal("test.cs", context.FilePath);
        Assert.Equal("content", context.FileContent);
        Assert.Equal("selected", context.SelectedText);
        Assert.Equal(5, context.CaretLine);
        Assert.Equal(10, context.CaretColumn);
    }
}
