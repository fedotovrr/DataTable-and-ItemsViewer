using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;
using ItemsViewer;

namespace DataTable
{
    //Style
    public partial class Table : ItemsViewerSelectable, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private Brush bordersBrush = Brushes.DarkGray;
        private Brush headerBackground = Brushes.Silver;
        private Brush focusCellBorderBrush = Brushes.RoyalBlue;
        private Brush selectBackground = Brushes.LightGray;
        private Brush dropOverBackground = Brushes.Gray;
        private Brush headerForeground = Brushes.Black;
        private Brush iconColor = Brushes.DodgerBlue;
        private Brush enabledIconColor = Brushes.LightGray;

        public Brush HeaderBackground { get => headerBackground; set { if (headerBackground != value) { headerBackground = value; NotifyPropertyChanged(); } } }

        public Brush BordersBrush { get => bordersBrush; set { if (bordersBrush != value) { bordersBrush = value; NotifyPropertyChanged(); } } }

        public Brush FocusCellBorderBrush { get => focusCellBorderBrush; set { if (focusCellBorderBrush != value) { focusCellBorderBrush = value; NotifyPropertyChanged(); } } }

        public Brush SelectBackground { get => selectBackground; set { if (selectBackground != value) { selectBackground = value; NotifyPropertyChanged(); } } }

        public Brush DropOverBackground { get => dropOverBackground; set { if (dropOverBackground != value) { dropOverBackground = value; NotifyPropertyChanged(); } } }

        public Brush HeaderForeground { get => headerForeground; set { if (headerForeground != value) { headerForeground = value; NotifyPropertyChanged(); } } }

        public Brush IconColor { get => iconColor; set { if (iconColor != value) { iconColor = value; NotifyPropertyChanged(); } } }

        public Brush EnabledIconColor { get => enabledIconColor; set { if (enabledIconColor != value) { enabledIconColor = value; NotifyPropertyChanged(); } } }
    }
}