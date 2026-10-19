using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml.Linq;

namespace KrnlAI.Desktop.Tests.Controls;

[CollectionDefinition("WPF screens", DisableParallelization = true)]
public sealed class ScreenRuntimeCollection;

[Collection("WPF screens")]
public sealed class ScreenRuntimeTests
{
    private static string AppRoot([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "../../../src/KrnlAI.Desktop.App"));

    [Fact]
    public void AllScreens_CompiledXaml_ShouldConstructAndLayoutInBothThemes()
    {
        RunSta(() =>
        {
            var app = new Application();
            try
            {
                var document = XDocument.Load(Path.Combine(AppRoot(), "App.xaml"));
                XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
                var resources = document.Descendants(wpf + "ResourceDictionary").First();
                foreach (var attribute in document.Root!.Attributes().Where(a => a.IsNamespaceDeclaration))
                    resources.SetAttributeValue(attribute.Name, attribute.Value.StartsWith("clr-namespace:", StringComparison.Ordinal)
                        ? attribute.Value + ";assembly=KrnlAI.Desktop" : attribute.Value);
                foreach (var element in resources.Descendants().Where(e => e.Name.NamespaceName.StartsWith("clr-namespace:", StringComparison.Ordinal)))
                    element.Name = XName.Get(element.Name.LocalName, element.Name.NamespaceName + ";assembly=KrnlAI.Desktop");
                resources.Descendants(wpf + "ResourceDictionary").First().SetAttributeValue("Source",
                    "/KrnlAI.Desktop;component/Resources/Themes/Dark.xaml");
                app.Resources = (ResourceDictionary)XamlReader.Parse(resources.ToString());
                var failures = new List<string>();
                var combo = new ComboBox { Style = (Style)app.Resources[typeof(ComboBox)], ItemsSource = new[] { "Português", "English" } };
                combo.ApplyTemplate();
                if (combo.Template.FindName("PART_Popup", combo) is not System.Windows.Controls.Primitives.Popup)
                    failures.Add("ComboBox: dropdown template must contain PART_Popup so options can be selected.");
                foreach (var theme in new[] { "Dark", "Light" })
                {
                    app.Resources.MergedDictionaries[0] = new ResourceDictionary
                    {
                        Source = new Uri($"/KrnlAI.Desktop;component/Resources/Themes/{theme}.xaml", UriKind.Relative)
                    };
                    foreach (var type in typeof(ChatControl).Assembly.GetTypes()
                                 .Where(t => t.Namespace == typeof(ChatControl).Namespace && !t.IsAbstract && typeof(UserControl).IsAssignableFrom(t)))
                    {
                        try
                        {
                            var control = (UserControl)Activator.CreateInstance(type)!;
                            control.Measure(new Size(920, 720));
                            control.Arrange(new Rect(0, 0, 920, 720));
                            control.UpdateLayout();
                        }
                        catch (Exception ex)
                        {
                            failures.Add($"{theme}/{type.Name}: {ex.GetBaseException().Message}");
                        }
                    }
                }
                Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
            }
            finally { app.Shutdown(); }
        });
    }

    [Theory]
    [InlineData("SecurityIncidentsControl")]
    [InlineData("GovernanceControl")]
    [InlineData("NotificationsControl")]
    [InlineData("ProvenanceControl")]
    public void MainWindow_ShellBoundScreen_ShouldInheritShellContext(string controlName)
    {
        var document = XDocument.Load(Path.Combine(AppRoot(), "MainWindow.xaml"));
        var screen = document.Descendants().Single(e => e.Name.LocalName == controlName);
        Assert.Null(screen.Attribute("DataContext"));
    }

    [Fact]
    public void MainWindow_Navigation_ShouldAllowScrollingAtMinimumWindowSize()
    {
        var document = XDocument.Load(Path.Combine(AppRoot(), "MainWindow.xaml"));
        var navigation = document.Descendants().First(e => (string?)e.Attribute("Command") == "{Binding NavigateToChatCommand}");
        Assert.True(navigation.Ancestors().Any(e => e.Name.LocalName == "ScrollViewer" && (string?)e.Attribute("VerticalScrollBarVisibility") == "Auto"),
            "Navigation must scroll so all screens remain reachable at the minimum window height.");
    }

    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "WPF screen validation timed out.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
