using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using ItemsViewer;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataTable
{
    public class SearchControl : Border, INotifyPropertyChanged
    {
        private bool isSearch = true;
        private bool isReplace = true;
        private TextBox SearchText;
        private TextBox ReplaceText;
        private CheckBox Reg;

        private Table Table;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSearch { get => isSearch; set { if (isSearch != value) { isSearch = value; NotifyPropertyChanged(); } } }

        public bool IsReplace { get => isReplace; set { if (isReplace != value) { isReplace = value; NotifyPropertyChanged(); } } }

        public SearchControl(Table parent)
        {
            Table = parent;

            Width = 250;
            Grid.SetRow(this, 1);
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Top;
            Visibility = Visibility.Collapsed;
            FocusVisualStyle = null;
            PreviewKeyDown += SearchControl_PreviewKeyDown;
            BorderThickness = new Thickness(1);
            BorderBrush = Brushes.Gray;
            Background = Brushes.LightGray;
            Margin = new Thickness(0,-1,0,0);

            Grid grid = new Grid();
            Child = grid;

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            GridSplitter gridSplitter = new GridSplitter() { Width = 5, HorizontalAlignment = HorizontalAlignment.Stretch, Focusable = false };
            gridSplitter.SetBinding(GridSplitter.BackgroundProperty, new Binding(nameof(Background)) { Source = this });
            gridSplitter.DragDelta += GridSplitter_DragDelta; Grid.SetColumn(gridSplitter, 0);
            grid.Children.Add(gridSplitter);

            StackPanel stackPanel = new StackPanel() { Margin = new Thickness(6) };
            Grid.SetColumn(stackPanel, 1);
            grid.Children.Add(stackPanel);

            Grid g1 = new Grid();
            TextBlock hb = new TextBlock() { Text = "Поиск и замена" };
            Path p = new Path() { Stretch = Stretch.None, Data = Geometry.Parse("M0,0 L8,8 M0,8 L8,0"), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, StrokeThickness = 1.5 };
            Border cb = new Border() { HorizontalAlignment = HorizontalAlignment.Right, Focusable = false, Background = new SolidColorBrush(new Color()), Child = p };
            Setter setterA = new Setter() { Property = Border.BorderBrushProperty, Value = Table.IconColor };
            Trigger trigger = new Trigger() { Property = Border.IsMouseOverProperty, Value = true, Setters = { setterA }, };
            Setter setterE = new Setter(Border.BorderBrushProperty, Table.EnabledIconColor);
            cb.Style = new Style(typeof(Border)) { Triggers = { trigger }, Setters = { setterE } };
            p.SetBinding(Path.StrokeProperty, new Binding(nameof(Border.BorderBrush)) { Source = cb });

            cb.MouseDown += SearchCloseButton_Click;
            g1.Children.Add(hb);
            g1.Children.Add(cb);

            Grid g2 = new Grid() { Margin = new Thickness(0, 8, 0, 0) };
            g2.SetBinding(GridSplitter.IsEnabledProperty, new Binding(nameof(IsSearch)) { Source = this });
            g2.ColumnDefinitions.Add(new ColumnDefinition() { MaxWidth = 45 });
            g2.ColumnDefinitions.Add(new ColumnDefinition());
            g2.ColumnDefinitions.Add(new ColumnDefinition(){ MaxWidth = 70 });
            TextBlock pt = new TextBlock() { Text = "Поиск" };
            SearchText = new TextBox();
            Button pb = new Button() { Content = "Найти", Margin = new Thickness(5,0,0,0) };
            pb.Click += SearchButton_Click;
            Grid.SetColumn(SearchText, 1);
            Grid.SetColumn(pb, 2);
            g2.Children.Add(pt);
            g2.Children.Add(SearchText);
            g2.Children.Add(pb);

            Grid g3 = new Grid() { Margin = new Thickness(0, 5, 0, 0) };
            g3.SetBinding(GridSplitter.IsEnabledProperty, new Binding(nameof(IsReplace)) { Source = this });
            g3.ColumnDefinitions.Add(new ColumnDefinition() { MaxWidth = 45 });
            g3.ColumnDefinitions.Add(new ColumnDefinition());
            g3.ColumnDefinitions.Add(new ColumnDefinition() { MaxWidth = 70 });
            TextBlock rt = new TextBlock() { Text = "Замена" };
            ReplaceText = new TextBox();
            Button rb = new Button() { Content = "Заменить", Margin = new Thickness(5, 0, 0, 0) };
            rb.Click += ReplaceButton_Click;
            Grid.SetColumn(ReplaceText, 1);
            Grid.SetColumn(rb, 2);
            g3.Children.Add(rt);
            g3.Children.Add(ReplaceText);
            g3.Children.Add(rb);

            Reg = new CheckBox() { Content = "Учитывать регистр", Margin = new Thickness(0, 5, 0, 0) };

            stackPanel.Children.Add(g1);
            stackPanel.Children.Add(g2);
            stackPanel.Children.Add(g3);
            stackPanel.Children.Add(Reg);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Open()
        {
            Visibility = Visibility.Visible;
            SearchText?.Focus();
        }

        public void Close()
        {
            Visibility = Visibility.Collapsed;
            Table.Focus();
        }

        private void SearchCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SearchControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool handled = true;
            Key key = e.Key;
            switch (key)
            {
                case Key.Escape:
                    SearchCloseButton_Click(null, null);
                    break;
                case Key.Enter:
                    SearchButton_Click(null, null);
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

        private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double ns = ActualWidth - e.HorizontalChange;
            if (ns > 150 && ns < Table.ItemsBorder.ActualWidth)
                Width = ns;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Table table = Table;
            int iCount = table.ItemsCollection.Count;
            bool reg = Reg.IsChecked.Value;
            string sValue = reg ? SearchText.Text : SearchText.Text.ToLower();
            if (iCount == 0 || String.IsNullOrEmpty(sValue)) return;
            ITableRow row = table.FocusItem == null ? table.ItemsCollection.GetItem(0) as ITableRow : table.FocusItem.Source as ITableRow;
            ViewItem item = table.FocusItem == null ? table.ItemsCollection.GetViewItem(0) : table.FocusItem;
            if (row == null || item == null) return;
            int fc = Searcher(row, table.FocusColumn + 1, sValue, reg);
            if (fc > -1)
            {
                table.FocusColumn = fc;
                return;
            }
            for (int i = item.Index + 1; i < iCount; i++)
            {
                fc = Searcher(table.ItemsCollection.GetItem(i) as ITableRow, 0, sValue, reg);
                if (fc > -1)
                {
                    table.FocusColumn = fc;
                    table.SetFocus(table.ItemsCollection.GetViewItem(i).Index);
                    return;
                }
            }
            for (int i = 0; i <= item.Index; i++)
            {
                fc = Searcher(table.ItemsCollection.GetItem(i) as ITableRow, 0, sValue, reg);
                if (fc > -1)
                {
                    table.FocusColumn = fc;
                    table.SetFocus(table.ItemsCollection.GetViewItem(i).Index);
                    return;
                }
            }
        }

        private static int Searcher(ITableRow row, int startColumn, string sValue, bool reg)
        {
            if (row != null && row.GetRowInfo() is CellInfo[] cells)
                for (int i = startColumn; i < cells.Length; i++)
                    if ((reg ? cells[i].Value : cells[i].Value?.ToLower())?.Contains(sValue) == true)
                        return i;
            return -1;
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            Table table = Table;
            int iCount = table.ItemsCollection.Count;
            bool reg = Reg.IsChecked.Value;
            string sValue = reg ? SearchText.Text : SearchText.Text.ToLower();
            string rValue = ReplaceText.Text;
            if (iCount == 0 || String.IsNullOrEmpty(sValue)) return;
            List<UndoRedoManager.ReadCommand> comands = new List<UndoRedoManager.ReadCommand>();
            for (int j = 0; j < iCount; j++)
                if (table.ItemsCollection.GetItem(j) is ITableRow row && row.GetRowInfo() is CellInfo[] cells)
                    for (int i = 0; i < cells.Length; i++)
                        if (!cells[i].ReadOnly && cells[i].Value is string value && !string.IsNullOrEmpty(value))
                            if ((reg ? value.Replace(sValue, rValue) : ReplaceIgnoreCase(value, sValue, rValue)) is string newVal && newVal != value)
                            {
                                row.SetValue(i, newVal);
                                comands.Add(new UndoRedoManager.ReadCommand(row, value, newVal, i));
                            }
            if (table.IsUndoRedo && comands.Count > 0)
                table.UndoRedoManager.RegistredNewCommand(comands);
        }

        private static string ReplaceIgnoreCase(string input, string pattern, string replacement)
        {

            if (String.IsNullOrEmpty(input) || String.IsNullOrEmpty(pattern))
                return input;
            string il = input.ToLower();
            string pl = pattern.ToLower();
            string ret = input;
            for (int i = input.Length - pattern.Length; i >= 0; i--)
                if (il.Substring(i, pl.Length) == pl)
                    ret = input.Substring(0, i) + replacement + input.Substring(i + pl.Length, input.Length - (i + pl.Length));
            return ret;
        }
    }
}
