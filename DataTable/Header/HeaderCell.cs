using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.Collections;
using ItemsViewer.Collection;
using System.Threading;

namespace DataTable.Header
{
    public class HeaderCell : INotifyPropertyChanged
    {
        private Table Parent;
        private string text;
        private double width = 200;
        private int Column;
        private ObservableCollection<FilterItem> FilterItems = new ObservableCollection<FilterItem>();
        private Popup filterPopup;
        private ListView itemsControl;
        private TextBox newFilterText;
        private bool RefreshIsCheckedValues;

        public string Text { get => text; set { text = value; NotifyPropertyChanged(); } }

        public double Width { get => width; set { width = value; NotifyPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal HeaderCell(Table parent, int column)
        {
            this.Column = column;
            Parent = parent;

            string cn = Thread.CurrentThread.CurrentUICulture.Name;

            //текст ячейки
            TextBlock headerText = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(3, 0, 3, 0), TextTrimming = TextTrimming.CharacterEllipsis, TextAlignment = TextAlignment.Center };
            headerText.SetBinding(TextBlock.TextProperty, new Binding(nameof(Text)) { Source = this });
            headerText.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(Table.HeaderForeground)) { Source = Parent });
            Grid.SetColumn(headerText, 1);
            
            //иконка сортировки
            Path sortIcon = new Path() { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 3, 0), ToolTip = cn == "ru-RU" ? "Визуальная сортировка (порядок элементов сохраняется)" : "Visual sort" };
            sortIcon.SetBinding(Path.FillProperty, new Binding(nameof(Table.IconColor)) { Source = Parent });
            Grid.SetColumn(sortIcon, 2);
            MultiBinding multiBinding = new MultiBinding();
            multiBinding.Bindings.Add(new Binding(nameof(SortProperties.SortType)) { Source = Parent.ItemsCollection?.Sort });
            multiBinding.Bindings.Add(new Binding(nameof(SortProperties.Column)) { Source = Parent.ItemsCollection?.Sort });
            multiBinding.Bindings.Add(new Binding(nameof(Table.TrySort)) { Source = Parent });
            multiBinding.Converter = new SortIconConverter(column);
            sortIcon.SetBinding(Path.DataProperty, multiBinding);

            //иконка фильтра
            Path filterIcon = new Path() { Margin = new Thickness(3, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Data = Geometry.Parse("M 0,0 14,0 9,5 9,12 5,10 5,5"), ToolTip = cn == "ru-RU" ? "Визуальный фильтр" : "Visual filter" };
            filterIcon.SetBinding(Path.FillProperty, new Binding(nameof(FilterManager.Columns)) { Converter = new FilterBrushConverter(Parent, column), Source = Parent.ItemsCollection?.Filter });

            Border filterBorder = new Border() { Width = 20, Background = new SolidColorBrush(new Color()), Cursor = Cursors.Hand };
            filterBorder.SetBinding(Path.VisibilityProperty, new Binding(nameof(Table.TryFilter)) { Converter = new BooleanToVisibilityConverter(), Source = Parent });
            filterBorder.Child = filterIcon;
            filterBorder.MouseDown += ButtonFilter_Click;

            //панель фильтра
            TextBlock headerPanelText = new TextBlock() { Text = cn == "ru-RU" ? "Фильтр" : "Filter", HorizontalAlignment = HorizontalAlignment.Center };
            headerPanelText.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(Table.HeaderForeground)) { Source = Parent });

            newFilterText = new TextBox();
            Button addValue = new Button() { Content = cn == "ru-RU" ? "Добавить" : "Add", Width = 70, Margin = new Thickness(4,0,0,0) };
            Grid.SetColumn(addValue, 1);
            addValue.Click += AddValue_Click;
            Grid newValueGrid = new Grid() { Height = 22, Margin = new Thickness(0, 8, 0, 4) };
            newValueGrid.ColumnDefinitions.Add(new ColumnDefinition());
            newValueGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            newValueGrid.Children.Add(newFilterText);
            newValueGrid.Children.Add(addValue);
            Grid.SetRow(newValueGrid, 1);

            Border separator = new Border() { Height = 1, Background = Parent.BordersBrush, Margin = new Thickness(0, 4, 0, 4) };
            Grid.SetRow(separator, 2);

            FrameworkElementFactory itemFactory = new FrameworkElementFactory(typeof(CheckBox));
            itemFactory.SetValue(CheckBox.FocusVisualStyleProperty, null);
            itemFactory.SetBinding(CheckBox.ContentProperty, new Binding(nameof(FilterItem.Value)));
            itemFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(FilterItem.IsChecked)) { Mode = BindingMode.TwoWay });
            itemFactory.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(Item_Checked));
            itemFactory.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(Item_Checked));

            itemsControl = new ListView() { BorderThickness = new Thickness(0), Background = Brushes.Transparent, FocusVisualStyle = null };
            itemsControl.ItemTemplate = new DataTemplate() { DataType = typeof(CheckBox) };
            itemsControl.ItemTemplate.VisualTree = itemFactory;
            itemsControl.ItemsSource = FilterItems;
            Grid.SetRow(itemsControl, 3);

            Grid filterGrid = new Grid() { Margin = new Thickness(4) };
            filterGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            filterGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            filterGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            filterGrid.RowDefinitions.Add(new RowDefinition());
            filterGrid.Children.Add(headerPanelText);
            filterGrid.Children.Add(newValueGrid);
            filterGrid.Children.Add(separator);
            filterGrid.Children.Add(itemsControl);

            Border border = new Border() { Background = Parent.HeaderBackground, BorderThickness = new Thickness(1), BorderBrush = Parent.BordersBrush };
            border.Child = filterGrid;

            filterPopup = new Popup() { MaxHeight = 400, Width = width + 1, PlacementRectangle = new Rect(-1, -1, 0, 0) };
            filterPopup.SetBinding(Popup.PlacementTargetProperty, new Binding() { Source = filterBorder });
            filterPopup.Child = border;
            filterPopup.StaysOpen = false;
            filterPopup.AllowsTransparency = true;
            filterPopup.PreviewKeyDown += FilterPopup_PreviewKeyDown;
            filterPopup.Closed += FilterPopup_Closed;

            //сетка ячейки
            Grid gridCell = new Grid();
            gridCell.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            gridCell.ColumnDefinitions.Add(new ColumnDefinition());
            gridCell.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            gridCell.Children.Add(headerText);
            gridCell.Children.Add(sortIcon);
            gridCell.Children.Add(filterBorder);

            //границы ячейки
            Border borderCell = new Border { Width = width, BorderThickness = new Thickness(0, 1, 1, 1) };
            borderCell.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Table.BordersBrush)) { Source = Parent });
            borderCell.SetBinding(Border.BackgroundProperty, new Binding(nameof(Table.HeaderBackground)) { Source = Parent });
            borderCell.SetBinding(Border.WidthProperty, new Binding(nameof(Width)) { Source = this, Mode = BindingMode.TwoWay });
            borderCell.SetBinding(Border.ToolTipProperty, new Binding(nameof(Text)) { Source = this });
            borderCell.Child = gridCell;
            borderCell.MouseDown += ButtonSort_Click;

            //сплиттер
            ColumnSplitter cellSplitter = new ColumnSplitter { Width = 6, Background = new SolidColorBrush(new Color { A = 0, R = 0, G = 0, B = 0 }), Margin = new Thickness(-6, 0, 0, 0) };
            cellSplitter.OnResize += CellSplitter_OnResize;

            //добавление в строку
            StackPanel headerRow = (StackPanel)Parent.HeaderContainer.Children[0];
            borderCell.ClipToBounds = false;
            headerRow.Children.Add(borderCell);
            headerRow.Children.Add(cellSplitter);
        }

        private void CellSplitter_OnResize(object sender, EventArgs e)
        {
            Parent.Refresh();
        }

        private void ButtonFilter_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            Parent?.EndEdit(true);
            RefreshFilterCollection();
        }

        private void RefreshFilterCollection()
        {
            if (Parent.ItemsCollection == null)
                return;
            filterPopup.Width = Width + 1;
            filterPopup.IsOpen = true;
            FilterItems.Clear();
            List<FilterItem> newItems = Parent.ItemsCollection.Filter.GetItems(Column);
            newItems.Sort(new SortProperties.NaturalStringComparer<FilterItem>(x => x.Value));
            for (int i = 0; i < newItems.Count; i++)
                FilterItems.Add(newItems[i]);
            itemsControl.Focus();
        }

        private void FilterPopup_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                filterPopup.IsOpen = false;
            }
            else if (e.Key == Key.Delete)
            {
                e.Handled = true;
                if (Parent.ItemsCollection.Filter.Remove(itemsControl.SelectedItems))
                    RefreshFilterCollection();
            }
        }

        private void AddValue_Click(object sender, RoutedEventArgs e)
        {
            if (newFilterText.Text is string av && !String.IsNullOrWhiteSpace(av))
            {
                FilterItem ni = new FilterItem(newFilterText.Text, Column, true);
                if (Parent.ItemsCollection.Filter.Add(ni))
                {
                    RefreshFilterCollection();
                    itemsControl.ScrollIntoView(ni);
                }
            }
            newFilterText.Text = null;
        }

        private void Item_Checked(object sender, RoutedEventArgs e)
        {
            if (!RefreshIsCheckedValues && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && (sender as CheckBox)?.IsChecked.Value is bool value)
            {
                RefreshIsCheckedValues = true;
                for (int i = 0; i < FilterItems.Count; i++)
                    FilterItems[i].IsChecked = value;
                RefreshIsCheckedValues = false;
            }
        }

        private void FilterPopup_Closed(object sender, EventArgs e)
        {
            //Parent?.ItemsCollection?.Filter.Refresh(FilterItems, Column);
        }

        private void ButtonSort_Click(object sender, RoutedEventArgs e)
        {
            if (!Parent.TrySort)
                return;
            Parent?.EndEdit(true);
            Parent?.ItemsCollection?.Sort.SwitchSort(Column);
        }
    }
}
