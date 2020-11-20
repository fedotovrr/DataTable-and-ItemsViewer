using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using DataTable;
using DataTable.Header;
using System.Collections.Specialized;

namespace ItemsViewer.Collection
{
    internal class FilterManager : INotifyPropertyChanged
    {
        private ViewCollection Source;

        public event PropertyChangedEventHandler PropertyChanged;

        private List<FilterItem> Filters = new List<FilterItem>();

        public ColumnGetter Column { get; private set; }

        public bool Any => Filters.Count > 0;

        internal FilterManager(ViewCollection source)
        {
            Source = source;
            Column = new ColumnGetter(this);
        }

        public bool CheckRow(CellInfo[] cells)
        {
            foreach (FilterItem item in Filters.Where(x => x.IsUser))
                if (item.Column >= 0 && item.Column < cells.Length && Contains(cells[item.Column].Value, item.Value))
                    return true;
            foreach (FilterItem item in Filters.Where(x => !x.IsUser))
                if (item.Column >= 0 && item.Column < cells.Length && cells[item.Column].Value == item.Value)
                    return false;
            return true;
        }

        private bool Contains(string source, string value)
        {
            if (String.IsNullOrEmpty(source) && String.IsNullOrEmpty(value)) return true;
            if (String.IsNullOrEmpty(source) || String.IsNullOrEmpty(value)) return false;
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void Refresh(IEnumerable<FilterItem> items, int column)
        {
            Filters = Filters.Where(x => x.Column != column).ToList();
            foreach (FilterItem item in items)
                if ((item.IsUser ? item.IsChecked : !item.IsChecked) && Filters.FirstOrDefault(x => x.Column == item.Column && x.IsUser == item.IsUser && x.Value == item.Value) == null)
                    Filters.Add(item);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Column)));
            Source.CollectionChanged_CollectionChanged(Source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
        }

        public List<FilterItem> GetItems(int column)
        {
            return Filters.Where(x => x.Column == column).ToList();
        }

        internal class ColumnGetter
        {
            private FilterManager Source;

            internal ColumnGetter(FilterManager source)
            {
                Source = source;
            }

            public bool IsFilterColumnContains(int column)
            {
                return Source.Filters.FirstOrDefault(x => x.Column == column) != null;
            }
        }
    }
}
