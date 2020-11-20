namespace DataTable
{
    public interface ITableRow
    {
        string[] GetEditValues(int column);

        CellInfo[] GetRowInfo();

        void SetValue(int column, string value);
    }
}