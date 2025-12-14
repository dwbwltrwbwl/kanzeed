using System;
using System.Globalization;
using System.Windows.Data;

namespace kanzeed.Pages
{
    public class PriceGreaterThan1000Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal price)
                return price > 1000;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
