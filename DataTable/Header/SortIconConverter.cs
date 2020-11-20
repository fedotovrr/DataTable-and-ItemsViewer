using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ItemsViewer.Collection;

namespace DataTable.Header
{
    internal class SortIconConverter : IMultiValueConverter
    {
        private int Column;

        public SortIconConverter(int column)
        {
            Column = column;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length == 3 && values[0] is SortProperties.SortTypes type && values[1] is int column && values[2] is bool trySort &&
                trySort && column == Column)
            {
                if (type == SortProperties.SortTypes.Descending)
                    return Geometry.Parse("M 0,0 14,0 7,7");
                else if (type == SortProperties.SortTypes.Ascending)
                    return Geometry.Parse("M 7,0 14,7 0,7");
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
