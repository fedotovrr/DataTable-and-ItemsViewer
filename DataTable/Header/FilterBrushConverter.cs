using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using ItemsViewer.Collection;

namespace DataTable.Header
{
    internal class FilterBrushConverter : IValueConverter
    {
        private Table Source;
        private int Column;

        public FilterBrushConverter(Table source, int column)
        {
            Source = source;
            Column = column;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is FilterManager.ColumnGetter getter && getter.IsFilterColumnContains(Column) ? Source.IconColor : Source.EnabledIconColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
