using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
namespace ProjectHub.App.Controls;

public sealed class ListPathVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => App.Engine.Settings.ViewMode == "Compact" || !App.Engine.Settings.VisibleColumns.Contains("Path") ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
