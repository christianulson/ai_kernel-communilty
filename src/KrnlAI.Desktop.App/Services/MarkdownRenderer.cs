using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace KrnlAI.Desktop.App.Services;

public sealed class MarkdownRenderer
{
    private static readonly Brush CodeBg = new SolidColorBrush(Color.FromArgb(20, 100, 180, 255));
    private static readonly Brush PreBg = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));

    static MarkdownRenderer()
    {
        CodeBg.Freeze();
        PreBg.Freeze();
    }

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
                var tb = FormattedText(line[4..]);
                tb.FontSize = 15;
                tb.FontWeight = FontWeights.SemiBold;
                tb.Margin = new Thickness(0, 8, 0, 4);
                stack.Children.Add(tb);
                continue;
            }
            if (line.StartsWith("## "))
            {
                var tb = FormattedText(line[3..]);
                tb.FontSize = 17;
                tb.FontWeight = FontWeights.Bold;
                tb.Margin = new Thickness(0, 10, 0, 4);
                stack.Children.Add(tb);
                continue;
            }
            if (line.StartsWith("# "))
            {
                var tb = FormattedText(line[2..]);
                tb.FontSize = 20;
                tb.FontWeight = FontWeights.Bold;
                tb.Margin = new Thickness(0, 12, 0, 6);
                stack.Children.Add(tb);
                continue;
            }

            // Unordered list
            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                var tb = FormattedText(line[2..]);
                tb.Text = "  \u2022  " + tb.Text;
                tb.TextWrapping = TextWrapping.Wrap;
                tb.Margin = new Thickness(0, 1, 0, 1);
                stack.Children.Add(tb);
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
            var paragraph = FormattedText(line);
            paragraph.TextWrapping = TextWrapping.Wrap;
            paragraph.Margin = new Thickness(0, 2, 0, 2);
            paragraph.LineHeight = 24;
            stack.Children.Add(paragraph);
        }

        // Close unclosed code block
        if (inCodeBlock && codeBlockLines.Count > 0)
        {
            stack.Children.Add(RenderCodeBlock(string.Join("\n", codeBlockLines), codeBlockLang));
        }

        return stack;
    }

    private static TextBlock FormattedText(string text)
    {
        var tb = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var regex = new System.Text.RegularExpressions.Regex(@"(\*\*(.+?)\*\*|\*(.+?)\*|`(.+?)`)");
        int lastIndex = 0;

        foreach (System.Text.RegularExpressions.Match match in regex.Matches(text))
        {
            if (match.Index > lastIndex)
                tb.Inlines.Add(new Run(text[lastIndex..match.Index]));

            if (match.Groups[2].Success)
                tb.Inlines.Add(new Run(match.Groups[2].Value) { FontWeight = FontWeights.Bold });
            else if (match.Groups[3].Success)
                tb.Inlines.Add(new Run(match.Groups[3].Value) { FontStyle = FontStyles.Italic });
            else if (match.Groups[4].Success)
                tb.Inlines.Add(new Run(match.Groups[4].Value)
                {
                    FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
                    Background = CodeBg
                });

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
            tb.Inlines.Add(new Run(text[lastIndex..]));

        return tb;
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
