using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Snipdeck.App.Converters
{
    /// <summary>
    /// Visible when the bound integer is greater than zero. Used to hide
    /// sections such as "Most used" when the underlying collection is empty.
    /// </summary>
    public sealed partial class CountToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var hasItems = value is int count && count > 0;
            if (Invert)
            {
                hasItems = !hasItems;
            }
            return hasItems ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
