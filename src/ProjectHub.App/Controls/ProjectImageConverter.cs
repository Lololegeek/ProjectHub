using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
namespace ProjectHub.App.Controls;

public sealed class ProjectImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string path || !File.Exists(path))
            return null;
        if (ProjectHub.Core.Utilities.SafeFileSystem.IsReparse(path) || new FileInfo(path).Length > 10_000_000)
            return null;
        try
        {
            return new BitmapImage(new Uri(Path.GetFullPath(path))) { DecodePixelWidth = 96 };
        }
        catch (ArgumentException) { return null; }
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}
