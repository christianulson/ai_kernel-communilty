using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace KrnlAI.Desktop.Tests.ViewModels;

public class ViewModelBindingTests
{
    private static readonly string AppRoot = FindDesktopAppRoot();

    private static readonly Assembly ViewModelAssembly = typeof(KrnlAI.Desktop.App.ViewModels.MainViewModel).Assembly;

    [Fact]
    public void RadioButtonBindings_ToReadOnlyProperties_ShouldUseOneWay()
    {
        var issues = new List<string>();
        var controlFiles = Directory.GetFiles(Path.Combine(AppRoot, "Controls"), "*.xaml");

        foreach (var file in controlFiles)
        {
            var content = File.ReadAllText(file);
            var radioBindings = Regex.Matches(content,
                @"<RadioButton[^>]*IsChecked=""\{Binding\s+([^,}]+)(,[^}]*)?\}""");

            foreach (Match m in radioBindings)
            {
                var binding = m.Groups[1].Value.Trim();
                var fullBinding = m.Groups[0].Value;

                var propName = binding.Split('.').Last();
                var viewModelType = ResolveViewModelType(binding);

                if (viewModelType == null) continue;

                var prop = viewModelType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                {
                    issues.Add($"{Path.GetFileName(file)}: binding '{binding}' — property '{propName}' not found on {viewModelType.Name}");
                    continue;
                }

                if (!prop.CanWrite && !fullBinding.Contains("Mode=OneWay") && !fullBinding.Contains("Mode=OneTime"))
                {
                    issues.Add($"{Path.GetFileName(file)}: '{binding}' is read-only but uses TwoWay binding (add Mode=OneWay)");
                }
            }
        }

        if (issues.Count > 0)
            Assert.Fail($"Found {issues.Count} binding issue(s):\n  {string.Join("\n  ", issues)}");
    }

    [Fact]
    public void TextBoxBindings_ToReadOnlyProperties_ShouldUseOneWay()
    {
        var issues = new List<string>();
        var controlFiles = Directory.GetFiles(Path.Combine(AppRoot, "Controls"), "*.xaml");

        foreach (var file in controlFiles)
        {
            var content = File.ReadAllText(file);
            var textBindings = Regex.Matches(content,
                @"<TextBox[^>]*Text=""\{Binding\s+([^,}]+)(,[^}]*)?\}""");

            foreach (Match m in textBindings)
            {
                var binding = m.Groups[1].Value.Trim();
                var fullBinding = m.Groups[0].Value;

                var propName = binding.Split('.').Last();
                var viewModelType = ResolveViewModelType(binding);

                if (viewModelType == null) continue;

                var prop = viewModelType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                {
                    issues.Add($"{Path.GetFileName(file)}: binding '{binding}' — property '{propName}' not found on {viewModelType.Name}");
                    continue;
                }

                if (!prop.CanWrite && !fullBinding.Contains("Mode=OneWay") && !fullBinding.Contains("Mode=OneTime"))
                {
                    issues.Add($"{Path.GetFileName(file)}: '{binding}' is read-only but uses TwoWay binding (add Mode=OneWay)");
                }
            }
        }

        if (issues.Count > 0)
            Assert.Fail($"Found {issues.Count} binding issue(s):\n  {string.Join("\n  ", issues)}");
    }

    [Fact]
    public void AllBindings_ReferToExistingProperties()
    {
        var issues = new List<string>();
        var controlFiles = Directory.GetFiles(Path.Combine(AppRoot, "Controls"), "*.xaml");

        foreach (var file in controlFiles)
        {
            var content = File.ReadAllText(file);
            var bindings = Regex.Matches(content,
                @"\{Binding\s+([^}]+)\}");

            foreach (Match m in bindings)
            {
                var binding = m.Groups[1].Value.Trim();
                if (binding.Contains(", ")) continue;

                var parts = binding.Split('.');
                if (parts.Length is < 2 or > 2) continue;

                var viewModelName = parts[^2];
                var propName = parts[^1];

                var viewModelType = ResolveViewModelType(binding);

                if (viewModelType == null) continue;

                var prop = viewModelType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null)
                {
                    issues.Add($"{Path.GetFileName(file)}: '{binding}' — property '{propName}' not found on {viewModelType.Name}");
                }
            }
        }

        if (issues.Count > 0)
            Assert.Fail($"Found {issues.Count} binding issue(s):\n  {string.Join("\n  ", issues)}");
    }

    private static Type? ResolveViewModelType(string binding)
    {
        var parts = binding.Split('.');
        var vmPrefix = parts.Length >= 2 ? parts[^2] : null;

        if (vmPrefix == null) return null;

        var viewModelName = vmPrefix.EndsWith("VM", StringComparison.Ordinal)
            ? vmPrefix[..^2] + "ViewModel"
            : vmPrefix + "ViewModel";

        var types = ViewModelAssembly.GetTypes()
            .Where(t => t.Name.Equals(viewModelName, StringComparison.OrdinalIgnoreCase)
                        && t.Namespace?.Contains("ViewModels") == true)
            .ToList();

        return types.Count == 1 ? types[0] : null;
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
