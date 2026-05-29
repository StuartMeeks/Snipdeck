using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Snipdeck.App.Converters
{
    public sealed partial class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var flag = value is bool b && b;
            if (Invert)
            {
                flag = !flag;
            }
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
