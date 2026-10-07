using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace ProjectHub.App.Controls;

public static class PageElements
{
    public static TextBlock Heading(string text) => new() { Text = text, FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
    public static TextBlock Text(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
    public static TextBlock Section(string text) => new() { Text = text, FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 4) };
    public static Button Button(string label, Action action)
    {
        var b = new Button { Content = label };
        b.Click += (_, _) => action();
        return b;
    }
    public static StackPanel Column() => new() { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
    public static StackPanel Row(params UIElement[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var c in children)
            row.Children.Add(c);
        return row;
    }
    public static ScrollViewer Scroll(StackPanel panel) => new() { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0, 0, 16, 20) };
}
