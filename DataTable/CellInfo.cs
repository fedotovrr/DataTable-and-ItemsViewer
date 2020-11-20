using System.Windows;
using System.Windows.Media;

namespace DataTable
{
    public class CellInfo
    {
        public string Value { get; set; }

        public bool ReadOnly { get; set; }

        public bool MultiEdit { get; set; } = true;

        public int ColumnSpan { get; set; } = 1;

        public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

        public TextWrapping TextWrapping { get; set; } = TextWrapping.Wrap;

        public Color Background { get; set; } = Colors.Transparent;

        internal static int GetFocusColumn(CellInfo[] cells, int targetColumn)
        {
            if (cells == null || cells.Length == 0) return 0;
            int column = targetColumn < 0 ? 0 : targetColumn >= cells.Length ? cells.Length - 1 : targetColumn;
            int sum = 0;
            for (int i = 0; i < cells.Length; i += cells[i].ColumnSpan)
            {
                sum += cells[i].ColumnSpan;
                if (sum > column)
                {
                    column = i;
                    break;
                }
            }
            return column;
        }
    }
}