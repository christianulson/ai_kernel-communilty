using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace KrnlAI.Desktop.Tests.ViewModels;

public sealed class ScreenBindingContractTests
{
    private static string AppRoot([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "../../../src/KrnlAI.Desktop.App"));

    [Fact]
    public void Screens_ShellBindings_ShouldResolveEveryPropertyPath()
    {
        var root = AppRoot();
        var main = XDocument.Load(Path.Combine(root, "MainWindow.xaml"));
        var errors = new List<string>();
        foreach (var screen in main.Descendants().Where(e => e.Name.NamespaceName.EndsWith(".Controls", StringComparison.Ordinal)))
        {
            var file = Path.Combine(root, "Controls", screen.Name.LocalName + ".xaml");
            if (!File.Exists(file)) continue;
            var context = typeof(MainViewModel);
            var contextPath = (string?)screen.Attribute("DataContext");
            if (contextPath != null)
            {
                var propertyName = Regex.Match(contextPath, @"\{Binding\s+(\w+)").Groups[1].Value;
                var property = context.GetProperty(propertyName);
                if (property == null)
                {
                    errors.Add($"{screen.Name.LocalName}: shell has no {propertyName}");
                    continue;
                }
                context = property.PropertyType;
            }
            foreach (var element in XDocument.Load(file).Descendants())
            {
                if (element.Name.LocalName.EndsWith("Column", StringComparison.Ordinal)) continue; // DataGrid columns bind to their row model.
                if (element.AncestorsAndSelf().Any(e => e.Name.LocalName is "DataTemplate" or "ControlTemplate" or "Style" || e.Attribute("DataContext") != null)) continue;
                foreach (var attribute in element.Attributes())
                {
                    var match = Regex.Match(attribute.Value, @"^\{Binding\s+(?:Path=)?([\w.]+)");
                    if (!match.Success || attribute.Value.Contains("RelativeSource", StringComparison.Ordinal) || attribute.Value.Contains("ElementName", StringComparison.Ordinal)) continue;
                    var type = context;
                    foreach (var segment in match.Groups[1].Value.Split('.'))
                    {
                        var property = type.GetProperty(segment);
                        if (property == null)
                        {
                            errors.Add($"{screen.Name.LocalName}: {match.Groups[1].Value} ({type.Name} has no {segment})");
                            break;
                        }
                        type = property.PropertyType;
                    }
                }
            }
        }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors.Distinct()));
    }

    [Fact]
    public void AppStartup_XamlOwnedViewModel_ShouldNotReplaceWindowDataContext()
    {
        var source = File.ReadAllText(Path.Combine(AppRoot(), "App.xaml.cs"));
        Assert.DoesNotContain("_mainWindow.DataContext = new", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AdminUsersControl", "AdminUsersVM.SelectedUser")]
    [InlineData("DocumentControl", "DocumentVM.SelectedDocument")]
    public void Screen_DetailActions_ShouldAllowSelectingAnItem(string screen, string selection)
    {
        var document = XDocument.Load(Path.Combine(AppRoot(), "Controls", screen + ".xaml"));
        Assert.True(document.Descendants().Any(e => e.Name.LocalName == "ListBox" &&
            ((string?)e.Attribute("SelectedItem"))?.Contains(selection, StringComparison.Ordinal) == true),
            $"{screen} must expose a selectable list bound to {selection}.");
    }

    [Theory]
    [InlineData("GovernanceControl", "GovernanceVM.LoadCommand")]
    [InlineData("NotificationsControl", "NotificationsVM.LoadCommand")]
    public void Screen_RemoteData_ShouldExposeRefresh(string screen, string command)
    {
        var document = XDocument.Load(Path.Combine(AppRoot(), "Controls", screen + ".xaml"));
        Assert.True(document.Descendants().Any(e => ((string?)e.Attribute("Command")) == "{Binding " + command + "}"),
            $"{screen} must allow loading and retrying remote data.");
    }
}
