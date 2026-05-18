using System.Windows;
using System.Windows.Media;
using KrnlAI.Desktop.App.Converters;

namespace KrnlAI.Desktop.Tests.Converters;

public class BoolToVisibilityConverterTests
{
    private readonly BoolToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_True_ShouldBeVisible()
    {
        var result = _converter.Convert(true, typeof(Visibility), null!, null!);
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_False_ShouldBeCollapsed()
    {
        var result = _converter.Convert(false, typeof(Visibility), null!, null!);
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_NonBool_ShouldBeCollapsed()
    {
        var result = _converter.Convert("not bool", typeof(Visibility), null!, null!);
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(Visibility.Visible, typeof(bool), null!, null!));
    }
}

public class InverseBoolConverterTests
{
    private readonly InverseBoolConverter _converter = new();

    [Fact]
    public void Convert_True_ShouldBeFalse()
    {
        var result = _converter.Convert(true, typeof(bool), null!, null!);
        Assert.False((bool)result);
    }

    [Fact]
    public void Convert_False_ShouldBeTrue()
    {
        var result = _converter.Convert(false, typeof(bool), null!, null!);
        Assert.True((bool)result);
    }

    [Fact]
    public void ConvertBack_True_ShouldBeFalse()
    {
        var result = _converter.ConvertBack(true, typeof(bool), null!, null!);
        Assert.False((bool)result);
    }

    [Fact]
    public void ConvertBack_False_ShouldBeTrue()
    {
        var result = _converter.ConvertBack(false, typeof(bool), null!, null!);
        Assert.True((bool)result);
    }
}

public class NullToVisibilityConverterTests
{
    private readonly NullToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_NonNull_ShouldBeVisible()
    {
        var result = _converter.Convert("something", typeof(Visibility), null!, null!);
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_Null_ShouldBeCollapsed()
    {
        var result =         _converter.Convert(null!, typeof(Visibility), null!, null!);
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(Visibility.Visible, typeof(object), null!, null!));
    }
}

public class BoolToStatusConverterTests
{
    private readonly BoolToStatusConverter _converter = new();

    [Fact]
    public void Convert_True_ShouldBeOnline()
    {
        var result = _converter.Convert(true, typeof(string), null!, null!);
        Assert.Equal("Online", result);
    }

    [Fact]
    public void Convert_False_ShouldBeOffline()
    {
        var result = _converter.Convert(false, typeof(string), null!, null!);
        Assert.Equal("Offline", result);
    }

    [Fact]
    public void Convert_NonBool_ShouldBeUnknown()
    {
        var result = _converter.Convert("x", typeof(string), null!, null!);
        Assert.Equal("Desconhecido", result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack("Online", typeof(bool), null!, null!));
    }
}

public class StatusToColorConverterTests
{
    private readonly StatusToColorConverter _converter = new();

    [Fact]
    public void Convert_Completed_ShouldBeGreen()
    {
        var result = _converter.Convert("completed", typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void Convert_Error_ShouldBeRed()
    {
        var result = _converter.Convert("error", typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void Convert_Unknown_ShouldBeDefaultColor()
    {
        var result = _converter.Convert("unknown", typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void Convert_NonString_ShouldBeDefaultColor()
    {
        var result = _converter.Convert(123, typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(new SolidColorBrush(Colors.Green), typeof(string), null!, null!));
    }
}

public class ScoreToColorConverterTests
{
    private readonly ScoreToColorConverter _converter = new();

    [Fact]
    public void Convert_HighScore_ShouldBeGreen()
    {
        var result = _converter.Convert(0.9, typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void Convert_MidScore_ShouldBeYellow()
    {
        var result = _converter.Convert(0.6, typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void Convert_LowScore_ShouldBeRed()
    {
        var result = _converter.Convert(0.3, typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void Convert_FloatScore_ShouldWork()
    {
        var result = _converter.Convert(0.8f, typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void Convert_NonNumeric_ShouldBeDefault()
    {
        var result = _converter.Convert("bad", typeof(Brush), null!, null!);
        Assert.IsAssignableFrom<Brush>(result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(new SolidColorBrush(Colors.Green), typeof(double), null!, null!));
    }
}

public class PercentToWidthConverterTests
{
    private readonly PercentToWidthConverter _converter = new();

    [Fact]
    public void Convert_FiftyPercent_ShouldBeHalfWidth()
    {
        var result = _converter.Convert(0.5, typeof(double), null!, null!);
        Assert.IsType<double>(result);
        Assert.True((double)result > 0);
    }

    [Fact]
    public void Convert_FloatValue_ShouldWork()
    {
        var result = _converter.Convert(0.3f, typeof(double), null!, null!);
        Assert.IsType<double>(result);
    }

    [Fact]
    public void Convert_Zero_ShouldReturnMinimum()
    {
        var result = _converter.Convert(0.0, typeof(double), null!, null!);
        Assert.Equal(2.0, (double)result, 1);
    }

    [Fact]
    public void Convert_NonNumeric_ShouldReturnMinimum()
    {
        var result = _converter.Convert("bad", typeof(double), null!, null!);
        Assert.Equal(2.0, result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(100.0, typeof(double), null!, null!));
    }
}

public class StringToVisibilityConverterTests
{
    private readonly StringToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_NonEmptyString_ShouldBeVisible()
    {
        var result = _converter.Convert("hello", typeof(Visibility), null!, null!);
        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void Convert_Null_ShouldBeCollapsed()
    {
        var result = _converter.Convert(null!, typeof(Visibility), null!, null!);
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_EmptyString_ShouldBeCollapsed()
    {
        var result = _converter.Convert(string.Empty, typeof(Visibility), null!, null!);
        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(Visibility.Visible, typeof(string), null!, null!));
    }
}

public class LocalizationConverterTests
{
    private readonly LocalizationConverter _converter = new();

    [Fact]
    public void Convert_WithParameterKey_ShouldReturnKey()
    {
        var result = _converter.Convert(null!, typeof(string), "test.key", null!);
        Assert.Equal("test.key", result);
    }

    [Fact]
    public void Convert_WithValueKey_ShouldReturnKey()
    {
        var result = _converter.Convert("fallback.key", typeof(string), null!, null!);
        Assert.Equal("fallback.key", result);
    }

    [Fact]
    public void Convert_NullKey_ShouldBeEmpty()
    {
        var result = _converter.Convert(null!, typeof(string), null!, null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack("localized", typeof(string), null!, null!));
    }
}

public class ListeningButtonConverterTests
{
    private readonly ListeningButtonConverter _converter = new();

    [Fact]
    public void Convert_True_ShouldBeStop()
    {
        var result = _converter.Convert(true, typeof(string), null!, null!);
        Assert.Equal("Parar Escuta", result);
    }

    [Fact]
    public void Convert_False_ShouldBeStart()
    {
        var result = _converter.Convert(false, typeof(string), null!, null!);
        Assert.Equal("Iniciar Escuta", result);
    }

    [Fact]
    public void Convert_NonBool_ShouldBeDefault()
    {
        var result = _converter.Convert(123, typeof(string), null!, null!);
        Assert.Equal("Escuta", result);
    }

    [Fact]
    public void ConvertBack_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack("start", typeof(bool), null!, null!));
    }
}
