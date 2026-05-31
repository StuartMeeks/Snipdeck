using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Snipdeck.App.Converters
{
    /// <summary>
    /// Visible when the bound string is non-empty, Collapsed otherwise — so
    /// status/error text takes no layout space until there's something to show.
    /// </summary>
    public sealed partial class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
