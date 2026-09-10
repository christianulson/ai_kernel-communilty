using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace KrnlAI.VisualStudio.Extensibility.Tests;

public sealed class ExtensionManifestTests
{
    private const string ExpectedExtensionId = "KrnlAI.VisualStudio.a6b3f8e1-2c4d-4e5f-8a9b-0c1d2e3f4a5b";

    private static string GetOutputRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "KrnlAI.VisualStudio.Extensibility", "bin", "Release", "net8.0"));

    [Fact]
    public void ExtensionManifest_ReleaseOutput_ShouldContainExtensionEntryPoint()
    {
        var manifestPath = Path.Combine(GetOutputRoot(), ".vsextension", "extension.json");

        File.Exists(manifestPath).Should().BeTrue(
            $"o build do projeto novo-modelo deve gerar extension.json em '{manifestPath}'");

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var entryPoint = doc.RootElement.GetProperty("services")[0].GetProperty("entryPoint");

        entryPoint.GetProperty("fullClassName").GetString()
            .Should().Be("KrnlAI.VisualStudio.Extensibility.KrnlAIExtension");
    }

    [Fact]
    public void ExtensionManifest_VsixManifest_ShouldKeepLegacyExtensionId()
    {
        var manifestPath = Path.Combine(GetOutputRoot(), "extension.vsixmanifest");

        File.Exists(manifestPath).Should().BeTrue();
        var content = File.ReadAllText(manifestPath);

        content.Should().Contain($"Id=\"{ExpectedExtensionId}\"");
        content.Should().Contain("Publisher=\"Krnl-AI\"");
        content.Should().Contain("ExtensionType=\"VisualStudio.Extensibility\"");
        content.Should().Contain("[17.14,)");
    }
}