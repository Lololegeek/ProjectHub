using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace ProjectHub.App.Controls;
public sealed partial class SettingRow : UserControl
{
    public static readonly DependencyProperty TitleProperty=DependencyProperty.Register(nameof(Title),typeof(string),typeof(SettingRow),new PropertyMetadata(""));
    public static readonly DependencyProperty DescriptionProperty=DependencyProperty.Register(nameof(Description),typeof(string),typeof(SettingRow),new PropertyMetadata(""));
    public static readonly DependencyProperty GlyphProperty=DependencyProperty.Register(nameof(Glyph),typeof(string),typeof(SettingRow),new PropertyMetadata(""));
    public static readonly DependencyProperty EditorProperty=DependencyProperty.Register(nameof(Editor),typeof(FrameworkElement),typeof(SettingRow),new PropertyMetadata(null));
    public string Title { get=>(string)GetValue(TitleProperty); set=>SetValue(TitleProperty,value); }
    public string Description { get=>(string)GetValue(DescriptionProperty); set=>SetValue(DescriptionProperty,value); }
    public string Glyph { get=>(string)GetValue(GlyphProperty); set=>SetValue(GlyphProperty,value); }
    public FrameworkElement? Editor { get=>(FrameworkElement?)GetValue(EditorProperty); set=>SetValue(EditorProperty,value); }
    public SettingRow()
    {
        InitializeComponent();
        SizeChanged+=(_,e)=>
        {
            bool narrow=e.NewSize.Width<650;
            Grid.SetColumn(EditorHost,narrow?1:2); Grid.SetRow(EditorHost,narrow?1:0); Grid.SetColumnSpan(EditorHost,narrow?2:1);
            EditorHost.HorizontalAlignment=narrow?HorizontalAlignment.Left:HorizontalAlignment.Right;
            EditorHost.Margin=narrow?new Thickness(0,12,0,0):new Thickness(0);
        };
    }
}
