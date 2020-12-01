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
using System.Collections;

namespace ItemsViewer.Collection
{
    internal class FilterManager : INotifyPropertyChanged, INotifyCollectionChanged
    {
        private ViewCollection Source;

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private List<FilterItem> Filters = new List<FilterItem>();

        public ColumnGetter Columns { get; private set; }

        public bool Any => Filters.Count > 0;
        
        internal FilterManager(ViewCollection source)
        {
            Source = source;
            Columns = new ColumnGetter(this);
        }

        public bool IsVisibleRow(CellInfo[] cells)
        {
            foreach (FilterItem item in Filters)
                if (item.IsChecked && item.Column >= 0 && item.Column < cells.Length && !Contains(cells[item.Column].Value, item.Value))
                    return false;
            return true;
        }

        private bool Contains(string source, string value)
        {
            if (String.IsNullOrEmpty(source) && String.IsNullOrEmpty(value)) return true;
            if (String.IsNullOrEmpty(source) || String.IsNullOrEmpty(value)) return false;
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public bool Add(FilterItem item)
        {
            if (Filters.FirstOrDefault(x => x.Equals(item)) == null)
            {
                Filters.Add(item);
                item.PropertyChanged += Item_PropertyChanged;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<FilterItem> { item }));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Columns)));
                Source.CollectionChanged_CollectionChanged(Source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
                return true;
            }
            return false;
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterItem.IsChecked))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Columns)));
                Source.CollectionChanged_CollectionChanged(Source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
            }
        }

        public bool Remove(IList items)
        {
            bool change = false;
            foreach (FilterItem item in items)
                if (Filters.Remove(item))
                {
                    change = true;
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            if (change)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Columns)));
                Source.CollectionChanged_CollectionChanged(Source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
            }
            return change;
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

            public bool Contains(int column)
            {
                return Source.Filters.FirstOrDefault(x => x.IsChecked && x.Column == column) != null;
            }
        }
    }
}
