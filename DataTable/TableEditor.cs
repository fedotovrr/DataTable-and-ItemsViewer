using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ItemsViewer;

namespace DataTable
{
    //Редакторивание
    public partial class Table : ItemsViewerSelectable, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private EditBox EditBox;
        private int EditColumn;

        public event SelectionChangedEventHandler EditBoxSelectionChanged;

        public ITableRow EditItem { get; private set; }

        internal bool IsEditing => EditBox != null;


        private void EditorOnKeyDown(KeyEventArgs e)
        {
            if (e.Handled)
                return;
            bool handled = true;
            Key key = e.Key;
            switch (key)
            {
                case Key.F2:
                    Edit();
                    break;
                case Key.Escape:
                    EndEdit(false);
                    break;
                case Key.Enter:
                    if (EditBox == null)
                        Edit();
                    else
                        EndEdit(true);
                    break;
                case Key.Back:
                    Edit();
                    if (EditBox != null && EditBox.eTextBox != null)
                        EditBox.eTextBox.Text = "";
                    break;
                default:
                    handled = false;
                    break;
            }
            if (handled)
                e.Handled = true;
        }

        private void Table_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (EditBox != null || Keyboard.Modifiers != ModifierKeys.None) return;
            Edit();
            if (EditBox != null && EditBox.eTextBox != null && !EditBox.eTextBox.IsReadOnly)
            {
                EditBox.eTextBox.Text += e.Text;
                EditBox.eTextBox.CaretIndex = EditBox.eTextBox.Text.Length;
            }
        }

        private void Table_FocusItemChanged(object sender, ViewItem e)
        {
            EndEdit(true);
        }

        private void Table_ViewRefreshed(object sender, EventArgs e)
        {
            RefreshEdit();
        }

        private void RefreshEdit()
        {
            if (EditBox == null) return;
            if (EditBox.Parent != null)
                ((Grid)EditBox.Parent).Children.Remove(EditBox);
            for (int i = 0; i < ItemsPanel.Children.Count; i++)
                if (ItemsPanel.Children[i] is RowControl ti && ti.Visibility == Visibility.Visible && ti.DataContext == FocusItem)
                {
                    ti?.Cells[EditColumn]?.Children.Add(EditBox);
                    EditBox.eTextBox.Focus();
                    return;
                }
        }

        internal void Edit()
        {
            if (EditBox != null)
                EndEdit(true);
            if (FocusItem == null)
                return;
            ScrollIntoView(FocusItem.Index);
            EditItem = FocusItem.Source as ITableRow;
            if (EditItem != null && EditItem.GetRowInfo() is CellInfo[] cells && CellInfo.GetFocusColumn(cells, FocusColumn) is int editColumn && editColumn >= 0 && editColumn < cells.Length)
            {
                EditColumn = editColumn;
                EditBox = new EditBox(cells[EditColumn], EditItem.GetEditValues(EditColumn));
                EditBox.SelectionChanged += EditBox_SelectionChanged;
                RefreshEdit();
            }
            else
                EditItem = null;
        }

        internal void EndEdit(bool confirm)
        {
            if (EditBox == null)
                return;
            if (EditBox.Parent != null)
                ((Grid)EditBox.Parent).Children.Remove(EditBox);
            string newText = EditBox.NewText;
            string oldText = EditBox.OldText;
            EditBox.SelectionChanged -= EditBox_SelectionChanged;
            EditBox = null;
            if (confirm && EditItem != null)
            {
                List<UndoRedoManager.ReadCommand> comands = new List<UndoRedoManager.ReadCommand>();
                if (EditItem.GetRowInfo() is CellInfo[] cells && EditColumn >= 0 && EditColumn < cells.Length && cells[EditColumn].MultiEdit)
                    for (int i = 0; i < ItemsCollection.Count; i++)
                        if (ItemsCollection.GetViewItem(i) is ViewItem viewItem && viewItem.Selected && ItemsCollection.GetItem(i) is ITableRow item && item != EditItem)
                        {
                            string oldValue = item.GetRowInfo() is CellInfo[] ci && ci.Length > 0 && EditColumn < ci.Length ? ci[EditColumn].Value : null;
                            item.SetValue(EditColumn, newText);
                            comands.Add(new UndoRedoManager.ReadCommand(item, oldValue, newText, EditColumn));
                        }
                EditItem.SetValue(EditColumn, newText);
                comands.Add(new UndoRedoManager.ReadCommand(EditItem, oldText, newText, EditColumn));
                IsActual = false;
                if (IsUndoRedo)
                    UndoRedoManager.RegistredNewCommand(comands);
            }
            this.Focus();
        }

        private void EditBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EditBoxSelectionChanged?.Invoke(sender, e);
        }
    }
}