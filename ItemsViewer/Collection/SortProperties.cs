using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using DataTable;

namespace ItemsViewer.Collection
{
    public class SortProperties : INotifyPropertyChanged
    {
        private ViewCollection Source;
        private SortTypes sortType;
        private int column;

        public SortTypes SortType => sortType;

        public int Column => column;

        public event PropertyChangedEventHandler PropertyChanged;


        internal SortProperties(ViewCollection source)
        {
            Source = source;
        }

        public void SwitchSort(int column)
        {
            if (this.column != column)
            {
                this.column = column;
                NotifyPropertyChanged(nameof(SortProperties.Column));
            }
            else
            {
                if (sortType == SortTypes.None)
                    sortType = SortTypes.Ascending;
                else if (sortType == SortTypes.Ascending)
                    sortType = SortTypes.Descending;
                else if (sortType == SortTypes.Descending)
                    sortType = SortTypes.None;
                NotifyPropertyChanged(nameof(SortProperties.SortType));
            }
            Source.CollectionChanged_CollectionChanged(Source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Source = null;
        }

        public enum SortTypes
        {
            None = 0,
            Ascending = 1,
            Descending = 2,
        }

        internal Comparer GetComparer()
        {
            return new Comparer(column, SortType == SortTypes.Descending ? true : false);
        }

        internal class Comparer : IComparer
        {
            private int Column;
            private bool Invert;

            public Comparer(int column, bool invert)
            {
                Column = column;
                Invert = invert;
            }

            public int Compare(object x, object y)
            {
                CellInfo[] xa = ((Tuple<object, CellInfo[]>)x).Item2;
                CellInfo[] ya = ((Tuple<object, CellInfo[]>)y).Item2;
                if (xa != null && (Column >= xa.Length || String.IsNullOrEmpty(xa[Column].Value))) xa = null;
                if (ya != null && (Column >= ya.Length || String.IsNullOrEmpty(ya[Column].Value))) ya = null;
                int r = 0;
                if (xa != null && ya != null)
                {
                    if (Double.TryParse(xa[Column].Value, out double xdv) && Double.TryParse(ya[Column].Value, out double ydv))
                        r = xdv == ydv ? 0 : xdv > ydv ? 1 : -1;
                    else
                        r = SafeNativeMethods.StrCmpLogicalW(xa[Column].Value, ya[Column].Value);
                }
                else if (xa == null && ya != null)
                    r = -1;
                else if (xa != null && ya == null)
                    r = 1;
                return Invert ? r == 0 ? 0 : r == 1 ? -1 : 1 : r;
            }
        }

        internal sealed class NaturalStringComparer<T> : IComparer<T>
        {
            private Func<T, string> KeySelector;

            public NaturalStringComparer(Func<T, string> keySelector)
            {
                KeySelector = keySelector;
            }

            public int Compare(T x, T y)
            {
                return SafeNativeMethods.StrCmpLogicalW(KeySelector(x), KeySelector(y));
            }
        }

        [SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
            public static extern int StrCmpLogicalW(string psz1, string psz2);
        }
    }
}
