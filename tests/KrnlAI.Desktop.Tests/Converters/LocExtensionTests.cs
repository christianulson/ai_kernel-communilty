using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using KrnlAI.Desktop.App.Converters;
using KrnlAI.Desktop.Core.Services;
using Moq;
using Xunit;

namespace KrnlAI.Desktop.Tests.Converters;

public sealed class LocExtensionTests
{
    private sealed class FakeLocalizationService(Dictionary<string, string> strings) : ILocalizationService
    {
        public string CurrentCulture => "pt-BR";

        public event EventHandler<string>? CultureChanged;

        public string GetString(string key) => strings.TryGetValue(key, out var value) ? value : $"[{key}]";

        public void SetCulture(string culture) => CultureChanged?.Invoke(this, culture);

        public IEnumerable<string> GetAvailableCultures() => ["pt-BR", "en"];
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
        thread.Join();

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void WithLocalization(string key, string value, Action action)
    {
        var previous = ServiceLocatorAccess.GetLocalizationService();
        try
        {
            ServiceLocatorAccess.SetLocalizationService(new FakeLocalizationService(new()
            {
                [key] = value
            }));
            action();
        }
        finally
        {
            if (previous is not null)
                ServiceLocatorAccess.SetLocalizationService(previous);
        }
    }

    [Fact]
    public void ProvideValue_OnTextBlockText_ShouldReturnBindingExpressionAndBindValue()
    {
        WithLocalization("sidebar_brand", "Krnl-AI", () => RunSta(() =>
        {
            var textBlock = new TextBlock();
            var target = new Mock<IProvideValueTarget>();
            target.SetupGet(x => x.TargetObject).Returns(textBlock);
            target.SetupGet(x => x.TargetProperty).Returns(TextBlock.TextProperty);
            var provider = new Mock<IServiceProvider>();
            provider.Setup(x => x.GetService(typeof(IProvideValueTarget))).Returns(target.Object);

            var result = new LocExtension("sidebar_brand").ProvideValue(provider.Object);

            // Regressão: o loader de XAML trata um Binding cru como valor literal e
            // lança "'Binding' is not a valid value for property 'Text'".
            Assert.IsAssignableFrom<BindingExpressionBase>(result);
            textBlock.SetValue(TextBlock.TextProperty, result);
            Assert.Equal("Krnl-AI", textBlock.Text);
        }));
    }

    [Fact]
    public void XamlReader_WithLocExtension_ShouldLoadTextBlockWithLocalizedText()
    {
        WithLocalization("sidebar_brand", "Krnl-AI", () => RunSta(() =>
        {
            var xaml = """
                <TextBlock
                    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:loc="clr-namespace:KrnlAI.Desktop.App.Converters;assembly=KrnlAI.Desktop"
                    Text="{loc:Loc sidebar_brand}" />
                """;

            var textBlock = Assert.IsType<TextBlock>(XamlReader.Parse(xaml));

            Assert.Equal("Krnl-AI", textBlock.Text);
        }));
    }

    [Fact]
    public void ProvideValue_MissingKey_ShouldReturnFallbackString()
    {
        var result = new LocExtension("").ProvideValue(new Mock<IServiceProvider>().Object);

        Assert.Equal("[MISSING KEY]", result);
    }
}
