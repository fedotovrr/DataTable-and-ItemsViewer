using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq;

namespace DataTable.Header
{
    public class HeaderCellSelector
    {
        private Table Parent;
        private List<HeaderCell> cells = new List<HeaderCell>();

        public HeaderCell this[int index]
        {
            get
            {
                if (index >= cells.Count)
                    for (int i = cells.Count; i <= index; i++)
                        cells.Add(new HeaderCell(Parent, i));
                return index < 0 ? null : cells[index];
            }
        }

        public int Count => cells.Count;

        internal HeaderCellSelector(Table parent)
        {
            Parent = parent;
            StackPanel container = (StackPanel)Parent.HeaderContainer.Children[0];
            container.Children.Clear();
            Border b0 = new Border { Width = 1, BorderThickness = new Thickness(1) };
            b0.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Table.BordersBrush)) { Source = Parent });
            container.Children.Add(b0);
            container.Height = 26;
        }

        public void SetText(params string[] args)
        {
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                    this[i].Text = args[i];
        }

        public void SetWidth(params double[] args)
        {
            if (args != null)
                for (int i = 0; i < args.Length && i < Count; i++)
                    this[i].Width = args[i];
        }

        public void SetWidth(params int[] args)
        {
            if (args != null)
                for (int i = 0; i < args.Length && i < Count; i++)
                    this[i].Width = args[i];
        }

        public double[] GetDoubleWidth()
        {
            return cells == null ? new double[0] : cells.Select(x => x.Width).ToArray();
        }

        public int[] GetInegerWidth()
        {
            return cells == null ? new int[0] : cells.Select(x => (int)x.Width).ToArray();
        }

        public string[] GetHeaderText()
        {
            return cells == null ? new string[0] : cells.Select(x => x.Text).ToArray();
        }
    }
}
