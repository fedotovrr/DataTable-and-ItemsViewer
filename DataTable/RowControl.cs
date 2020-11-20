using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ItemsViewer;

namespace DataTable
{
    public partial class RowControl : Grid
    {
        public static DataTemplate Template
        {
            get
            {
                DataTemplate r = new DataTemplate() { DataType = typeof(RowControl), VisualTree = new FrameworkElementFactory(typeof(RowControl)) };
                r.Seal();
                return r;
            }
        }

        private Grid RowGrid = new Grid() { Margin = new Thickness(1,0,0,0) };
        private Border SelectBorder = new Border() { Visibility = Visibility.Collapsed };
        private Border DropOverBorder = new Border() { Visibility = Visibility.Collapsed };
        private Border FocusBorder = new Border() { BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 1, 1), Visibility = Visibility.Collapsed };
        private ToggleButton DroppedMarker = new ToggleButton() { Width = 9, Height = 9, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(3,6,3,3), Focusable = false };
        internal Table ParentTable;
        private object _DataContext;

        public CellSelector Cells;

        public event EventHandler CellCreated;

        private bool isDragCapture;
        private bool IsDragCapture
        {
            get => isDragCapture;
            set
            {
                if (isDragCapture == value) return;
                isDragCapture = value;
                RefreshDragOverMarker();
            }
        }

        public RowControl()
        {
            Background = new SolidColorBrush(new Color { A = 0, R = 0, G = 0, B = 0 });
            AllowDrop = true;
            
            DataContextChanged += TableItem_DataContextChanged;
            MouseMove += TableItem_MouseMove;
            DragOver += TableItem_DragOver;
            DragLeave += TableItem_DragLeave;
            PreviewDrop += TableItem_PreviewDrop;
            Children.Add(SelectBorder);
            Children.Add(DropOverBorder);
            Children.Add(RowGrid);
            SelectBorder.SetBinding(Border.VisibilityProperty, new Binding(nameof(ViewItem.Selected)) { Converter = new BooleanToVisibilityConverter() });
            FocusBorder.SetBinding(Border.VisibilityProperty, new Binding(nameof(ViewItem.Focused)) { Converter = new BooleanToVisibilityConverter() });
            DroppedMarker.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(IChildCollection.Dropped)) { Mode = BindingMode.TwoWay });
            DroppedMarker.SetBinding(ToggleButton.VisibilityProperty, new Binding(nameof(IChildCollection.DropMarkVisible)) { Converter = new BooleanToVisibilityConverter() });
            DroppedMarker.Click += DroppedMarker_Click;
            Grid.SetColumnSpan(FocusBorder, 2);

            Cells = new CellSelector(this);
        }

        private void DroppedMarker_Click(object sender, RoutedEventArgs e)
        {
            if (ParentTable != null && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && _DataContext is ViewItemCollection vc)
                ParentTable.SetDrop(vc);
        }


        //DragDrop

        private void TableItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (ParentTable != null && !ParentTable.IsEditing && DataContext is ViewItem viewItem && ParentTable.DragDropOptions.IsPossibleDrag && Mouse.LeftButton == MouseButtonState.Pressed && viewItem.Selected &&
               PointToScreen(Mouse.GetPosition(this)) is Point NPM && (Math.Abs(ParentTable.DragPoint.X - NPM.X) > 2 || Math.Abs(ParentTable.DragPoint.Y - NPM.Y) > 2))
            {
                List<object> items = ParentTable.GetSelectList<object>(out int level);
                ClipboardData data = new ClipboardData(level, ParentTable.ItemsCollection.SourceItemType, items);
                ParentTable.IsDoDragDrop = true;
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                if (data.IsRemove)
                    ParentTable.Remove(ParentTable.IsUndoRedo, false);
                ParentTable.IsDoDragDrop = false;
            }
        }

        private void TableItem_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (ParentTable != null && ParentTable.DragDropOptions.IsPossibleDrop &&
                ((ParentTable.DragDropOptions.IsDropFiles && e.Data.GetDataPresent(DataFormats.FileDrop)) ||
                (e.Data.GetDataPresent(typeof(ClipboardData).FullName) &&
                (ParentTable.DragDropOptions.DropType == DragDropOptions.DropTypes.Full || (ParentTable.DragDropOptions.DropType == DragDropOptions.DropTypes.Local && ParentTable.IsDoDragDrop)))))
            {
                IsDragCapture = true;
                return;
            }
            IsDragCapture = false;
            e.Effects = DragDropEffects.None;
        }

        private void TableItem_DragLeave(object sender, DragEventArgs e)
        {
            IsDragCapture = false;
        }

        private void TableItem_PreviewDrop(object sender, DragEventArgs e)
        {
            IsDragCapture = false;
            e.Handled = true;
            if (ParentTable != null && ParentTable.DragDropOptions.IsPossibleDrop && DataContext is ViewItem viewItem)
            {
                if (ParentTable.DragDropOptions.IsDropFiles && e.Data.GetData(DataFormats.FileDrop) is string[] files)
                {
                    int level = DataContext is IChildCollection collection ? collection.DropLevel : 1;
                    if (ParentTable.DragDropOptions.IsDropFiles && ParentTable.FilesToItem(files, ref level) is List<object> items)
                        ParentTable.InsertItems(null, items, level, viewItem);
                }
                else if (e.Data.GetData(typeof(ClipboardData).FullName) is ClipboardData dp && dp.GetObject(ParentTable.ItemsCollection.SourceItemType, true) is List<object> items &&
                    (ParentTable.DragDropOptions.DropType == DragDropOptions.DropTypes.Full || (ParentTable.DragDropOptions.DropType == DragDropOptions.DropTypes.Local && ParentTable.IsDoDragDrop)))
                {
                    if (ParentTable.IsDoDragDrop)
                        ParentTable.InsertItems(ParentTable, items, dp.DropLevel, viewItem);
                    else
                    {
                        dp.IsRemove = true;
                        ParentTable.InsertItems(null, items, dp.DropLevel, viewItem);
                    }
                }
            }
        }

        //View

        private void TableItem_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldChanged)
                oldChanged.PropertyChanged -= ViewContext_PropertyChanged;
            if (e.NewValue is INotifyPropertyChanged newChanged)
                newChanged.PropertyChanged += ViewContext_PropertyChanged;
            _DataContext = e.NewValue;
            Refresh();
        }

        private void ViewContext_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ViewItem.Selected) && e.PropertyName != nameof(ViewItem.Focused) && e.PropertyName != nameof(IChildCollection.Dropped))
                Refresh();
        }

        private void ParentTable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Table.FocusColumn))
                RefreshFocus();
        }

        private void Cell_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (ParentTable != null)
            {
                ParentTable.FocusColumn = RowGrid.Children.IndexOf(sender as UIElement);
                ParentTable.DragPoint = PointToScreen(Mouse.GetPosition(this));
            }
            if (e.ClickCount > 1)
            {
                ParentTable.Edit();
                e.Handled = true;
            }
        }

        private void RefreshFocus()
        {
            if (DataContext is ViewItem vi && vi.Source is ITableRow row && row.GetRowInfo() is CellInfo[] cells)
            {
                int focusColumn = CellInfo.GetFocusColumn(cells, ParentTable.FocusColumn);
                if (FocusBorder.Parent is Grid oldFocusCell && RowGrid.Children.IndexOf(oldFocusCell) != focusColumn)
                    oldFocusCell.Children.Remove(FocusBorder);
                if (FocusBorder.Parent == null)
                    ((Grid)RowGrid.Children[focusColumn]).Children.Add(FocusBorder);
            }
        }

        private void RefreshDragOverMarker()
        {
            if (IsDragCapture)
                DropOverBorder.Visibility = Visibility.Visible;
            else
                DropOverBorder.Visibility = Visibility.Collapsed;
        }

        public virtual void Refresh()
        {
            if (Dispatcher.Thread != Thread.CurrentThread)
            {
                try { Dispatcher.Invoke(new Action(() => { Refresh(); })); } catch (Exception) { }
                return;
            }
            if (ParentTable == null)
            {
                DependencyObject obj = VisualTreeHelper.GetParent(this);
                while (obj != null)
                {
                    if (obj is Table table)
                    {
                        ParentTable = table;
                        ParentTable.PropertyChanged += ParentTable_PropertyChanged;
                        SelectBorder.SetBinding(Border.BackgroundProperty, new Binding(nameof(Table.SelectBackground)) { Source = ParentTable });
                        FocusBorder.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Table.FocusCellBorderBrush)) { Source = ParentTable });
                        DropOverBorder.SetBinding(Border.BackgroundProperty, new Binding(nameof(Table.DropOverBackground)) { Source = ParentTable });
                        ContextMenu = ParentTable.GetItemContextMenu();
                        break;
                    }
                    else
                        obj = VisualTreeHelper.GetParent(obj);
                }
            }
            if (DataContext is ViewItem vi && vi.Source is ITableRow row && row.GetRowInfo() is CellInfo[] cells)
            {
                //Удаление лишних ячеек
                int childrenCount = RowGrid.Children.Count;
                if (childrenCount > cells.Length)
                {
                    RowGrid.Children.RemoveRange(cells.Length, childrenCount - cells.Length);
                    RowGrid.ColumnDefinitions.RemoveRange(cells.Length, childrenCount - cells.Length);
                }

                int sh = -1;
                int eh = -1;
                for (int i = 0; i < cells.Length; i++)
                {
                    //Добавление ячеек
                    if (i >= childrenCount)
                    {
                        ColumnDefinition cd = new ColumnDefinition();
                        if (ParentTable.IsNoneColumn)
                            cd.SetBinding(ColumnDefinition.WidthProperty, new Binding(nameof(ParentTable.ItemsBorder.ActualWidth)) { Source = ParentTable.ItemsBorder });
                        else
                            cd.SetBinding(ColumnDefinition.WidthProperty, new Binding(nameof(Header.HeaderCell.Width)) { Source = ParentTable.Header[i] });
                        RowGrid.ColumnDefinitions.Add(cd);

                        Grid newCell = new Grid() { Background = new SolidColorBrush(new Color { A = 0, R = 0, G = 0, B = 0 }), AllowDrop = true };
                        Grid container = new Grid();
                        Border border = new Border() { BorderThickness = new Thickness(1), Margin = new Thickness(-1, -1, 0, 0) };
                        TextBlock textBlock = new TextBlock() { TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(3,1,3,4) };
                        Grid.SetColumn(newCell, i);
                        container.Children.Add(textBlock);
                        newCell.Children.Add(border);
                        newCell.Children.Add(container);
                        RowGrid.Children.Add(newCell);
                        newCell.MouseDown += Cell_MouseDown;
                        border.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Table.BordersBrush)) { Source = ParentTable });
                        if (i == 0 && vi.Source is IChildCollection)
                        {
                            newCell.Children.Add(DroppedMarker);
                            ColumnDefinition dmcd = new ColumnDefinition();
                            newCell.ColumnDefinitions.Add(dmcd);
                            newCell.ColumnDefinitions.Add(new ColumnDefinition());
                            dmcd.SetBinding(ColumnDefinition.WidthProperty, new Binding(nameof(IChildCollection.DropLevel)) { Converter = new LevelToWidthConverter() });
                            Grid.SetColumnSpan(border, 2);
                            Grid.SetColumn(container, 2);
                        }
                        CellCreated?.Invoke(this, null);
                    }

                    //Обновление ячеек
                    Grid cell = RowGrid.Children[i] as Grid;
                    cell.Background = new SolidColorBrush(cells[i].Background);
                    TextBlock text = (TextBlock)((Grid)cell.Children[1]).Children[0];
                    text.Text = cells[i].Value;
                    text.TextWrapping = cells[i].TextWrapping;
                    text.TextAlignment = cells[i].TextAlignment;
                    Grid.SetColumnSpan(cell, (cells[i].ColumnSpan > 0 ? cells[i].ColumnSpan : 1));
                    if (i >= sh && i <= eh)
                        cell.Visibility = Visibility.Collapsed;
                    else
                    {
                        cell.Visibility = Visibility.Visible;
                        sh = i;
                        eh = i + cells[i].ColumnSpan - 1;
                    }
                }
                RefreshFocus();
                isDragCapture = false;
                RefreshDragOverMarker();
            }
            else
            {
                RowGrid.Children.Clear();
                RowGrid.ColumnDefinitions.Clear();
            }
        }

        public class CellSelector
        {
            private RowControl source;

            public int Count => source.RowGrid.Children.Count;

            public Grid this[int index]
            {
                get
                {
                    if (source != null && index >= 0 && index < source.RowGrid.Children.Count)
                        return (Grid)((Grid)source.RowGrid.Children[index]).Children[1];
                    return null;
                }
            }

            internal CellSelector(RowControl source)
            {
                this.source = source;
            }
        }
    }
}