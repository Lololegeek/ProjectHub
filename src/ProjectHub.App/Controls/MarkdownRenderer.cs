using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
namespace ProjectHub.App.Controls;

public static class MarkdownRenderer
{
    public static void Render(StackPanel host, string markdown, string root)
    {
        host.Children.Clear();
        if (markdown.Length == 0)
        {
            host.Children.Add(new TextBlock { Text = "Aucun README détecté." });
            return;
        }
        bool code = false;
        var lines = new List<string>();
        void FlushCode()
        {
            if (lines.Count == 0)
                return;
            host.Children.Add(new Border { Padding = new Thickness(12), CornerRadius = new CornerRadius(6), Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"], Child = new TextBlock { Text = string.Join('\n', lines), FontFamily = new FontFamily("Cascadia Mono"), FontSize = 12, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true } });
            lines.Clear();
        }
        foreach (var line in markdown.Split('\n').Take(3000))
        {
            if (line.TrimStart().StartsWith("```"))
            {
                if (code)
                    FlushCode();
                code = !code;
                continue;
            }
            if (code)
            {
                lines.Add(line);
                continue;
            }
            if (string.IsNullOrWhiteSpace(line))
                continue;
            int heading = line.TakeWhile(x => x == '#').Count();
            string text = heading > 0 ? line[heading..].Trim() : line;
            if (text.StartsWith("- ") || text.StartsWith("* "))
                text = "• " + text[2..];
            var block = new RichTextBlock { TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true, FontSize = heading switch { 1 => 26, 2 => 21, 3 => 17, _ => 14 }, FontWeight = heading > 0 ? FontWeights.SemiBold : FontWeights.Normal };
            var paragraph = new Paragraph();
            int offset = 0;
            foreach (Match m in Regex.Matches(text, @"\[([^\]]+)\]\(([^)]+)\)|\*\*([^*]+)\*\*|`([^`]+)`"))
            {
                if (m.Index > offset)
                    paragraph.Inlines.Add(new Run { Text = text[offset..m.Index] });
                if (m.Groups[1].Success && Uri.TryCreate(m.Groups[2].Value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http")
                {
                    var link = new Hyperlink { NavigateUri = uri };
                    link.Inlines.Add(new Run { Text = m.Groups[1].Value });
                    paragraph.Inlines.Add(link);
                }
                else
                    paragraph.Inlines.Add(new Run { Text = m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Success ? m.Groups[4].Value : m.Value, FontWeight = m.Groups[3].Success ? FontWeights.Bold : FontWeights.Normal });
                offset = m.Index + m.Length;
            }
            if (offset < text.Length)
                paragraph.Inlines.Add(new Run { Text = text[offset..] });
            block.Blocks.Add(paragraph);
            host.Children.Add(block);
        }
        FlushCode();
    }
}
