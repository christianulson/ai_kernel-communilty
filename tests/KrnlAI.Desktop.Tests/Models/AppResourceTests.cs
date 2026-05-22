using System.Linq;
using System.IO.Packaging;
using System.Reflection;

namespace KrnlAI.Desktop.Tests.Models;

public class AppResourceTests
{
    private static readonly Assembly AppAssembly = typeof(KrnlAI.Desktop.App.App).Assembly;

    [Fact]
    public void PackUri_Resources_ShouldBeAccessible()
    {
        var testCases = new[]
        {
            "pack://application:,,,/KrnlAI.Desktop;component/Resources/Icons/krnl-ai-icon.png",
        };

        var failures = new List<string>();

        foreach (var uriStr in testCases)
        {
            try
            {
                var info = System.Windows.Application.GetResourceStream(new Uri(uriStr));
                if (info == null)
                    failures.Add($"'{uriStr}' returned null stream");
                else
                    using (info.Stream) { }
            }
            catch (Exception ex)
            {
                failures.Add($"'{uriStr}' threw: {ex.GetType().Name}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
            Assert.Fail(string.Join("\n", failures));
    }

    [Fact]
    public void PackUri_IconPng_ShouldBeAccessible()
    {
        var uri = new Uri("pack://application:,,,/KrnlAI.Desktop;component/Resources/Icons/krnl-ai-icon.png");
        var stream = System.Windows.Application.GetResourceStream(uri);
        Assert.NotNull(stream);
        using (stream.Stream) { }
    }

    [Fact]
    public void ThemeFiles_ShouldBeAccessible()
    {
        var themes = new[] { "Light.xaml", "Dark.xaml" };
        foreach (var theme in themes)
        {
            var uri = new Uri($"pack://application:,,,/KrnlAI.Desktop;component/Resources/Themes/{theme}");
            var ex = Record.Exception(() =>
            {
                var info = System.Windows.Application.GetResourceStream(uri);
                Assert.NotNull(info);
                using (info.Stream) { }
            });
            Assert.Null(ex);
        }
    }
}
