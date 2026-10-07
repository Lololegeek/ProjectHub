using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
namespace ProjectHub.App.Controls;

public sealed class ColumnWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string key = parameter?.ToString() ?? "";
        if (!App.Engine.Settings.VisibleColumns.Contains(key))
            return new GridLength(0);
        return key switch
        {
            "Stack" => new GridLength(2, GridUnitType.Star),
            "Git" => new GridLength(155),
            "Size" => new GridLength(90),
            _ => new GridLength(105)
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
