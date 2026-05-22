using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KrnlAI.Desktop.App.Services;

public sealed class MarkdownRenderer
{
    private static readonly Brush CodeBg = new SolidColorBrush(Color.FromArgb(20, 100, 180, 255));
    private static readonly Brush PreBg = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));

    public UIElement Render(string markdown)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            stack.Children.Add(new TextBlock { Text = markdown ?? "" });
            return stack;
        }

        var lines = markdown.Split('\n');
        var inCodeBlock = false;
        var codeBlockLines = new List<string>();
        var codeBlockLang = string.Empty;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Code blocks
            if (line.StartsWith("```"))
            {
                if (inCodeBlock)
                {
                    // End code block
                    stack.Children.Add(RenderCodeBlock(string.Join("\n", codeBlockLines), codeBlockLang));
                    codeBlockLines.Clear();
                    inCodeBlock = false;
                    continue;
                }
                inCodeBlock = true;
                codeBlockLang = line.Length > 3 ? line[3..].Trim() : "";
                continue;
            }

            if (inCodeBlock)
            {
                codeBlockLines.Add(line);
                continue;
            }

            // Empty line
            if (string.IsNullOrWhiteSpace(line))
            {
                stack.Children.Add(new TextBlock { Text = " ", FontSize = 4 });
                continue;
            }

            // Headers
            if (line.StartsWith("### "))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = FormattedText(line[4..]),
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 4)
                });
                continue;
            }
            if (line.StartsWith("## "))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = FormattedText(line[3..]),
                    FontSize = 17,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 4)
                });
                continue;
            }
            if (line.StartsWith("# "))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = FormattedText(line[2..]),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 12, 0, 6)
                });
                continue;
            }

            // Unordered list
            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "  \u2022  " + FormattedText(line[2..]),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 1, 0, 1)
                });
                continue;
            }

            // Numbered list
            if (line.Length > 2 && char.IsDigit(line[0]) && line[1] == '.')
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "  " + line,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 1, 0, 1)
                });
                continue;
            }

            // Horizontal rule
            if (line is "---" or "***" or "___")
            {
                stack.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)),
                    Margin = new Thickness(0, 8, 0, 8)
                });
                continue;
            }

            // Regular paragraph
            stack.Children.Add(new TextBlock
            {
                Text = FormattedText(line),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2),
                LineHeight = 24
            });
        }

        // Close unclosed code block
        if (inCodeBlock && codeBlockLines.Count > 0)
        {
            stack.Children.Add(RenderCodeBlock(string.Join("\n", codeBlockLines), codeBlockLang));
        }

        return stack;
    }

    private static string FormattedText(string text)
    {
        // Bold
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        // Italic
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "$1");
        // Inline code
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`(.+?)`", "$1");
        return text;
    }

    private static Border RenderCodeBlock(string code, string lang)
    {
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 300
        };

        var textBlock = new TextBlock
        {
            Text = code,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Padding = new Thickness(12, 10, 12, 10),
            Foreground = new SolidColorBrush(Color.FromRgb(200, 210, 220))
        };

        scrollViewer.Content = textBlock;

        return new Border
        {
            Child = scrollViewer,
            Background = PreBg,
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 6, 0, 6),
            BorderBrush = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128)),
            BorderThickness = new Thickness(1)
        };
    }
}
