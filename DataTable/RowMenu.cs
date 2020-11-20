using ItemsViewer;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DataTable
{
    public class RowMenu : ContextMenu
    {
        public MenuItem Cut = new MenuItem { Header = "Вырезать", InputGestureText = "Ctr+X" };
        public MenuItem Copy = new MenuItem { Header = "Копировать", InputGestureText = "Ctr+C" };
        public MenuItem Paste = new MenuItem { Header = "Вставить", InputGestureText = "Ctr+V" };
        public MenuItem Delete = new MenuItem { Header = "Удалить", InputGestureText = "Del" };

        public Table ParentTable => ((Parent as Popup)?.PlacementTarget as RowControl)?.ParentTable;

        public ViewItem ViewSource => ((Parent as Popup)?.PlacementTarget as RowControl)?.DataContext as ViewItem;

        public object Source => ViewSource?.Source;

        public RowMenu()
        {
            Items.Add(Cut);
            Items.Add(Copy);
            Items.Add(Paste);
            Items.Add(Delete);
            Cut.Click += (sender, e) => { ParentTable?.Cut(); };
            Copy.Click += (sender, e) => { ParentTable?.Copy(); };
            Paste.Click += (sender, e) => { ParentTable?.Paste(); };
            Delete.Click += (sender, e) => { ParentTable?.Remove(ParentTable.IsUndoRedo, false); };
            Opened += ContextMenu_Opened;
        }

        public virtual void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            Cut.Visibility = ParentTable != null && ParentTable.IsRemove && ParentTable.CopyOptions.IsPossibleCopy ? Visibility.Visible : Visibility.Collapsed;
            Copy.Visibility = ParentTable != null && ParentTable.CopyOptions.IsPossibleCopy ? Visibility.Visible : Visibility.Collapsed;
            Paste.Visibility = ParentTable != null && ParentTable.PasteOptions.IsPossiblePaste ? Visibility.Visible : Visibility.Collapsed;
            Delete.Visibility = ParentTable != null && ParentTable.IsRemove ? Visibility.Visible : Visibility.Collapsed;

            if (Copy.Visibility == Visibility.Visible)
                Paste.IsEnabled = (ParentTable.PasteOptions.PasteObject && Clipboard.GetData(typeof(ClipboardData).FullName) is ClipboardData data && data.GetObject(ParentTable?.ItemsCollection.SourceItemType, true) is List<object> items && items.Count > 0) ||
                    (ParentTable.PasteOptions.ParseText && Clipboard.GetDataObject()?.GetData(DataFormats.UnicodeText) is string str && !string.IsNullOrEmpty(str));
        }
    }
}
