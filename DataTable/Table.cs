using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ItemsViewer;
using DataTable.Header;

namespace DataTable
{
    //General
    public partial class Table : ItemsViewerSelectable, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public SearchControl SearchControl;
        internal UndoRedoManager UndoRedoManager;

        private int focusColumn;
        private bool trySort;
        private bool tryFilter;


        public event PropertyChangedEventHandler PropertyChanged;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public HeaderCellSelector Header { get; private set; }

        public int FocusColumn
        {
            get => focusColumn;
            set
            {
                int newValue = value < 0 ? 0 : Header == null ? value : value < Header.Count ? value : Header.Count - 1;
                if (focusColumn != newValue)
                {
                    focusColumn = newValue;
                    FocusColumnIntoView();
                    EndEdit(true);
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsUndoRedo { get; set; } = true;

        public bool TrySort { get => trySort; set { if (trySort != value) { trySort = value; NotifyPropertyChanged(); } } }

        public bool TryFilter { get => tryFilter; set { if (tryFilter != value) { tryFilter = value; NotifyPropertyChanged(); } } }

        public bool IsActual { get; set; }

        public bool IsCreateNewItems = true;

        internal bool IsNoneColumn;

        public Table()
        {
            DataTemplate d = new DataTemplate() { DataType = typeof(RowControl), VisualTree = new FrameworkElementFactory(typeof(RowControl)) };
            d.Seal();
            ItemTemplate = d;

            ViewRefreshed += Table_ViewRefreshed;
            FocusItemChanged += Table_FocusItemChanged;
            TextInput += Table_TextInput;
            ItemsBorder.DragOver += ItemsBorder_DragOver;
            ItemsBorder.Drop += ItemsBorder_Drop;
            ItemsSourceChanged += Table_ItemsSourceChanged;
            UndoRedoManager = new UndoRedoManager(this);

            HeaderContainer.Children.Add(new StackPanel() { Orientation = Orientation.Horizontal });
            Header = new HeaderCellSelector(this);

            SearchControl = new SearchControl(this);
            ControlGrid.Children.Add(SearchControl);
            Grid.SetRow(SearchControl, 1);
            SearchControl.SetBinding(Border.BackgroundProperty, new Binding(nameof(Table.HeaderBackground)) { Source = this });
            SearchControl.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Table.BordersBrush)) { Source = this });
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void InitNoneColumns()
        {
            ((StackPanel)HeaderContainer.Children[0]).Height = 0;
            IsNoneColumn = true;
        }

        private void Table_ItemsSourceChanged(object sender, EventArgs e)
        {
            string[] head = Header.GetHeaderText();
            double[] width = Header.GetDoubleWidth();
            Header = new HeaderCellSelector(this);
            Header.SetText(head);
            Header.SetWidth(width);
            UndoRedoClearBuffer();
        }

        public void UndoRedoClearBuffer()
        {
            UndoRedoManager.ClearBuffer();
        }

        private void FocusColumnIntoView()
        {
            if (FocusColumn < 0 || FocusColumn >= Header.Count)
                return;
            double sp = 0;
            for (int i = 0; i < FocusColumn; i++)
                sp += Header[i].Width;
            double ep = sp + Header[FocusColumn].Width;
            if (sp < ItemsHSB.Value)
                ItemsHSB_SetValue(sp);
            else if (ItemsHSB.Value + ItemsBorder.ActualWidth < ep)
                ItemsHSB_SetValue(ep - ItemsBorder.ActualWidth);
        }

        internal override void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (dropBlock) return;
            //if (e.PropertyName != nameof(ViewItem.Focused) && e.PropertyName != nameof(ViewItem.Selected) && e.PropertyName != nameof(IChildCollection.Dropped) && e.PropertyName != nameof(IChildCollection.DropMarkVisible))
            //    IsActual = false;
            base.Item_PropertyChanged(sender, e);
        }

        internal override void ItemCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (dropBlock) return;
            IsActual = false;
            if (!IsInserting && !IsRomoving && !UndoRedoManager.IsPerforming)
                UndoRedoManager.ClearBuffer();
            base.ItemCollection_CollectionChanged(sender, e);
            CollectionChanged?.Invoke(this, e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            EditorOnKeyDown(e);
            ItemManagerOnKeyDown(e);
            if (e.Handled) return;

            bool handled = true;
            Key key = e.Key;
            switch (key)
            {
                case Key.Left:
                    if (FocusItem?.Source is ITableRow row1 && row1.GetRowInfo() is CellInfo[] cells1)
                        FocusColumn = CellInfo.GetFocusColumn(cells1, CellInfo.GetFocusColumn(cells1, FocusColumn) - 1);
                    else
                        FocusColumn--;
                    ScrollIntoView(FocusItem);
                    break;
                case Key.Right:
                    if (FocusItem?.Source is ITableRow row2 && row2.GetRowInfo() is CellInfo[] cells2 && FocusColumn > 0 && FocusColumn < cells2.Length)
                        FocusColumn += cells2[FocusColumn].ColumnSpan;
                    else
                        FocusColumn++;
                    ScrollIntoView(FocusItem);
                    break;
                case Key.F:
                    if ((SearchControl.IsSearch || SearchControl.IsReplace) && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        SearchControl.Open();
                    else
                        handled = false;
                    break;
                case Key.Z:
                    if (IsUndoRedo && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        UndoRedoManager.Undo();
                    else
                        handled = false;
                    break;
                case Key.Y:
                    if (IsUndoRedo && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        UndoRedoManager.Redo();
                    else
                        handled = false;
                    break;
                default:
                    handled = false;
                    break;
            }
            if (handled)
                e.Handled = true;
            else
                base.OnKeyDown(e);
        }


        //Костыль дроб уровней через шифт

        private bool dropBlock = false;
        public void SetDrop(ViewItemCollection vc)
        {
            dropBlock = true;
            bool value = vc.Dropped;
            int count = ItemsCollection.SourceContainer.Count;
            for (int i = count - 1; i >= 0; i--)
                SetDrop(ItemsCollection.SourceContainer[i], vc.DropLevel, value);
            vc.Dropped = !value;
            dropBlock = false;
            vc.Dropped = value;
        }

        private void SetDrop(object item, byte level, bool value)
        {
            if (item is ViewItemCollection collection)
            {
                if (collection.DropLevel == level)
                    collection.Dropped = value;
                else if (collection.DropLevel < level)
                    for (int i = 0; i < collection.Count; i++)
                        SetDrop(collection[i], level, value);
            }
        }
    }
}