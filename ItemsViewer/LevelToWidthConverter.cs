using System;
using System.Globalization;
using System.Windows.Data;

namespace ItemsViewer
{
    public class LevelToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is byte v ? v == 0 ? 0 : (double)v * 10d + 6d : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
