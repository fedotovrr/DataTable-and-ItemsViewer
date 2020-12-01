using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataTable.Header
{
    internal class FilterItem : INotifyPropertyChanged
    {
        private string _value;
        private bool _isChecked = true;
        public bool IsUser;
        public int Column;

        public string Value { get => _value; set { if (_value != value) { _value = value; NotifyPropertyChanged(); } } }

        public bool IsChecked { get => _isChecked; set { if (_isChecked != value) { _isChecked = value; NotifyPropertyChanged(); } } }

        public event PropertyChangedEventHandler PropertyChanged;

        public FilterItem(string value, int column, bool isUser = false)
        {
            _value = value;
            Column = column;
            IsUser = isUser;
        }

        public bool Equals(FilterItem item)
        {
            return IsUser == item.IsUser && Column == item.Column && Value == item.Value;
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
