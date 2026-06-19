using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace KrnlAI.Desktop.Tests.Models;

public class XamlResourceTests
{
    private static readonly string AppRoot = FindDesktopAppRoot();

    [Fact]
    public void AllStaticResources_AreDefinedInThemeOrApp()
    {
        var themeFiles = Directory.GetFiles(
            Path.Combine(AppRoot, "Resources", "Themes"), "*.xaml");
        var appXaml = File.ReadAllText(Path.Combine(AppRoot, "App.xaml"));

        var knownResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var themeFile in themeFiles)
        {
            var content = File.ReadAllText(themeFile);
            foreach (Match m in Regex.Matches(content, @"x:Key=""([^""]+)"""))
                knownResources.Add(m.Groups[1].Value);
        }

        foreach (Match m in Regex.Matches(appXaml, @"x:Key=""([^""]+)"""))
            knownResources.Add(m.Groups[1].Value);

        var controlFiles = Directory.GetFiles(Path.Combine(AppRoot, "Controls"), "*.xaml");

        var extraDicts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();
        foreach (var controlFile in controlFiles)
        {
            var content = File.ReadAllText(controlFile);

            foreach (Match m in Regex.Matches(content, @"ResourceDictionary\s+Source=""([^""]+)"""))
            {
                var relPath = m.Groups[1].Value.TrimStart('/');
                var dictPath = Path.GetFullPath(Path.Combine(AppRoot, relPath));
                if (File.Exists(dictPath))
                    extraDicts.Add(dictPath);
            }

            foreach (Match m in Regex.Matches(content, @"{StaticResource\s+([^}""]+)}"))
            {
                var key = m.Groups[1].Value.Trim();
                if (!knownResources.Contains(key))
                    missing.Add($"{Path.GetFileName(controlFile)} references '{key}' not found in themes/App.xaml");
            }
        }

        foreach (var dictPath in extraDicts)
        {
            var content = File.ReadAllText(dictPath);
            foreach (Match m in Regex.Matches(content, @"x:Key=""([^""]+)"""))
                knownResources.Add(m.Groups[1].Value);
        }

        if (missing.Count > 0)
        {
            var recheck = new List<string>();
            foreach (var issue in missing)
                if (!knownResources.Contains(issue.Split('\'')[1]))
                    recheck.Add(issue);
            missing = recheck;
        }

        AssertEx(missing);
    }

    [Fact]
    public void TopLevelStaticResources_ShouldBeDefinedBeforeUsage()
    {
        var appXaml = File.ReadAllText(Path.Combine(AppRoot, "App.xaml"));

        var defined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var issues = new List<string>();
        var insideTemplate = 0;

        foreach (var line in appXaml.Split('\n'))
        {
            if (line.Contains("<ControlTemplate") || line.Contains("<Style"))
                insideTemplate++;
            if ((line.Contains("</ControlTemplate") || line.Contains("</Style>")) && insideTemplate > 0)
                insideTemplate--;

            if (insideTemplate > 0) continue;

            foreach (Match m in Regex.Matches(line, @"x:Key=""([^""]+)"""))
                defined.Add(m.Groups[1].Value);

            foreach (Match m in Regex.Matches(line, @"{StaticResource\s+([^}""]+)}"))
            {
                var key = m.Groups[1].Value.Trim();
                if (!defined.Contains(key) && !key.StartsWith("System:") &&
                    !key.StartsWith("ComponentResourceKey"))
                    issues.Add($"'{key}' used before defined in App.xaml (line: {line.Trim()})");
            }
        }

        AssertEx(issues);
    }

    [Fact]
    public void AllThemeKeys_AreConsistentBetweenLightAndDark()
    {
        var lightKeys = ExtractThemeKeys("Light.xaml");
        var darkKeys = ExtractThemeKeys("Dark.xaml");

        var onlyInLight = lightKeys.Except(darkKeys, StringComparer.OrdinalIgnoreCase).ToList();
        var onlyInDark = darkKeys.Except(lightKeys, StringComparer.OrdinalIgnoreCase).ToList();

        var diffs = new List<string>();
        diffs.AddRange(onlyInLight.Select(k => $"Light.xaml has '{k}' but Dark.xaml does not"));
        diffs.AddRange(onlyInDark.Select(k => $"Dark.xaml has '{k}' but Light.xaml does not"));

        AssertEx(diffs);
    }

    private HashSet<string> ExtractThemeKeys(string themeFileName)
    {
        var path = Path.Combine(AppRoot, "Resources", "Themes", themeFileName);
        var content = File.ReadAllText(path);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in Regex.Matches(content, @"x:Key=""([^""]+)"""))
            keys.Add(m.Groups[1].Value);
        return keys;
    }

    private static void AssertEx(List<string> issues)
    {
        if (issues.Count > 0)
            Assert.Fail($"Found {issues.Count} resource issue(s):\n  {string.Join("\n  ", issues)}");
    }

    private static string FindDesktopAppRoot([CallerFilePath] string sourceFilePath = "")
    {
        var sourceRelativeCandidate = Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(sourceFilePath)!, "..", "..", "..", "src", "KrnlAI.Desktop.App"));
        if (Directory.Exists(sourceRelativeCandidate))
            return sourceRelativeCandidate;

        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "Community", "src", "KrnlAI.Desktop.App");
            if (Directory.Exists(candidate))
                return candidate;

            candidate = Path.Combine(directory.FullName, "src", "KrnlAI.Desktop.App");
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Community/src/KrnlAI.Desktop.App from the test output directory.");
    }
}
