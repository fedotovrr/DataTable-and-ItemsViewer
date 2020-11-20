using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ItemsViewer;
using ItemsViewer.Collection;
using System.Linq;

namespace DataTable
{
    //Управление копированием, вставкой и перемещением элементов
    public partial class Table
    {
        public bool IsRemove { get; set; } = true;

        public CopyOptions CopyOptions { get; private set; } = new CopyOptions();

        public PasteOptions PasteOptions { get; private set; } = new PasteOptions();

        public DragDropOptions DragDropOptions { get; private set; } = new DragDropOptions();

        internal Point DragPoint = new Point();

        internal bool IsDoDragDrop;

        internal bool IsInserting;

        internal bool IsRomoving;

        public Func<ContextMenu> ItemContextMenu = new Func<ContextMenu>(() => new RowMenu());

        public Func<List<object>> FilesToItemConverter = new Func<List<object>>(() => new List<object>());


        private void ItemManagerOnKeyDown(KeyEventArgs e)
        {
            if (e.Handled)
                return;
            bool handled = true;
            Key key = e.Key;
            switch (key)
            {
                case Key.Delete:
                    Remove(IsUndoRedo, false);
                    break;
                case Key.X:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Cut();
                    else
                        handled = false;
                    break;
                case Key.C:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Copy();
                    else
                        handled = false;
                    break;
                case Key.V:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        Paste();
                    else
                        handled = false;
                    break;
                case Key.Insert:
                    if (IsCreateNewItems && ItemsSource != null)
                        InsertItems(null, new List<object>() { Activator.CreateInstance(ItemsCollection.SourceItemType) }, FocusItem is IChildCollection c ? c.DropLevel : 1, FocusItem);
                    else
                        handled = false;
                    break;
                default:
                    handled = false;
                    break;
            }
            if (handled)
                e.Handled = true;
        }

        //DragDrop

        private void ItemsBorder_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (DragDropOptions.IsPossibleDrop &&
                ((DragDropOptions.IsDropFiles && e.Data.GetDataPresent(DataFormats.FileDrop)) ||
                (e.Data.GetDataPresent(typeof(ClipboardData).FullName) &&
                (DragDropOptions.DropType == DragDropOptions.DropTypes.Full || (DragDropOptions.DropType == DragDropOptions.DropTypes.Local && IsDoDragDrop)))))
                return;
            e.Effects = DragDropEffects.None;
        }

        private void ItemsBorder_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if (DragDropOptions.IsPossibleDrop)
            {
                if (DragDropOptions.IsDropFiles && e.Data.GetData(DataFormats.FileDrop) is string[] files)
                {
                    int level = DataContext is IChildCollection collection ? collection.DropLevel : 1;
                    if (DragDropOptions.IsDropFiles && FilesToItem(files, ref level) is List<object> items)
                        InsertItems(null, items, level, null);
                }
                else if (e.Data.GetData(typeof(ClipboardData).FullName) is ClipboardData dp && dp.GetObject(ItemsCollection.SourceItemType, true) is List<object> items &&
                    (DragDropOptions.DropType == DragDropOptions.DropTypes.Full || (DragDropOptions.DropType == DragDropOptions.DropTypes.Local && IsDoDragDrop)))
                {
                    if (IsDoDragDrop)
                        InsertItems(this, items, dp.DropLevel, null);
                    else
                    {
                        dp.IsRemove = true;
                        InsertItems(null, items, dp.DropLevel, null);
                    }
                }
            }
        }

        //Items

        public virtual ContextMenu GetItemContextMenu()
        {
            return ItemContextMenu == null ? new RowMenu() : ItemContextMenu.Invoke();
        }

        internal void Cut()
        {
            Copy();
            Remove(IsUndoRedo, false);
        }

        internal List<UndoRedoManager.RemoveCommand> Remove(bool isRegUndoRedo, bool isGetUndoredo)
        {
            List<UndoRedoManager.RemoveCommand> commands = null;
            if (!IsRemove)
                return commands;
            IsRomoving = true;
            ViewItem fi = null;
            if (FocusItem != null)
                for (int i = FocusItem.Index - 1; i >= 0; i--)
                    if (ItemsCollection.GetViewItem(i) is ViewItem item && !item.Selected)
                    {
                        fi = item;
                        break;
                    }
            commands = ItemsCollection.Remove(GetSelectList<object>(out int level), isRegUndoRedo || isGetUndoredo) as List<UndoRedoManager.RemoveCommand>;
            int sindex = fi == null ? 0 : fi.Index + 1;
            if (commands != null && isRegUndoRedo)
                UndoRedoManager.RegistredNewCommand(commands);
            SetFocus(sindex);
            IsRomoving = false;
            return commands;
        }

        internal void Copy()
        {
            if (!CopyOptions.IsPossibleCopy) return;

            List<object> items = GetSelectList<object>(out int level);
            if (items == null || items.Count == 0) return;
            
            DataObject set = new DataObject();
            if (CopyOptions.SetClipboardObject)
            {
                ClipboardData data = new ClipboardData(level, ItemsCollection.SourceItemType, items);
                set.SetData(data);
            }
            if (CopyOptions.SetClipboardText)
            {
                string value = "";
                for (int i = 0; i < items.Count; i++)
                {
                    value += ConvertItemToString(items[i], level);
                    ConvertCollectionToString(items[i], level, ref value);
                }
                set.SetText(value, TextDataFormat.UnicodeText);
            }
            Clipboard.Clear();
            Clipboard.SetDataObject(set);
        }

        private void ConvertCollectionToString(object item, int level, ref string value)
        {
            level++;
            if (item is IChildCollection collection && collection.Count > 0)
                for (int i = 0; i < collection.Count; i++)
                {
                    value += ConvertItemToString(collection[i], level);
                    ConvertCollectionToString(collection[i], level, ref value);
                }
        }

        private string ConvertItemToString(object item, int level)
        {
            if (item is ITableRow row)
            {
                CellInfo[] cells = row.GetRowInfo();
                if (cells == null || cells.Length > 0)
                {
                    string str = "";
                    if (level != int.MaxValue)
                        str += level.ToString() + '\t';
                    for (int j = 0; j < cells.Length; j++)
                    {
                        string val = cells[j].Value;
                        if (String.IsNullOrEmpty(val))
                            val = " ";
                        else val = val?.Trim();
                        str += val + '\t';
                    }
                    str = str.Trim('\t') + "\r\n";
                    return str;
                }
            }
            return null;
        }

        internal void Paste()
        {
            //Вставка объекта из буфера
            if (PasteOptions.PasteObject)
                if (Clipboard.GetData(typeof(ClipboardData).FullName) is ClipboardData data && data.GetObject(ItemsCollection.SourceItemType, true) is List<object> items && items.Count > 0)
                {
                    InsertItems(null, items, data.DropLevel, FocusItem);
                    return;
                }

            //Преобразование текста из буфера в объекты
            if (PasteOptions.ParseText)
            {
                string str = Clipboard.GetDataObject()?.GetData(DataFormats.UnicodeText) as string;
                Type itemType = ItemsCollection.SourceItemType;
                if (String.IsNullOrEmpty(str) || itemType == null || itemType.GetInterface(typeof(ITableRow).FullName) == null)
                    return;

                string[] preArr = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string[][] tableArr = new string[preArr.Length][];
                for (int i = 0; i < preArr.Length; i++)
                    tableArr[i] = preArr[i].Split('\t');

                int[] dropLevels = new int[preArr.Length];
                bool isSetLevels = ItemsCollection.SourceItemsIsIChildCollection;
                if (isSetLevels)
                    for (int i = 0; i < tableArr.Length; i++)
                        if (tableArr[i].Length > 0)
                        {
                            if (Int32.TryParse(tableArr[i][0], out int droplevel) && droplevel >= 0)
                                dropLevels[i] = droplevel;
                            else
                            {
                                isSetLevels = false;
                                break;
                            }
                        }

                int level = int.MaxValue;
                List<object> items = new List<object>();
                if (isSetLevels)
                {
                    if (dropLevels.Length > 0)
                        level = dropLevels[0];
                    List<object> lasLevels = new List<object>();
                    for (int i = 0; i <= level; i++)
                        lasLevels.Add(null);
                    for (int i = 0; i < tableArr.Length; i++)
                    {
                        object obj = Activator.CreateInstance(itemType);
                        ITableRow ti = obj as ITableRow;
                        for (int j = 1; j < tableArr[i].Length; j++)
                            ti.SetValue(j - 1, tableArr[i][j]?.Trim());

                        if (dropLevels[i] == level)
                        {
                            items.Add(ti);
                            lasLevels[level] = ti;
                        }
                        else
                        {
                            if (lasLevels.Count > dropLevels[i] - 1)
                                (lasLevels[dropLevels[i] - 1] as IChildCollection)?.Add(ti);
                            if (dropLevels[i] < lasLevels.Count)
                                lasLevels[dropLevels[i]] = ti;
                            else if (dropLevels[i] == lasLevels.Count)
                                lasLevels.Add(ti);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < tableArr.Length; i++)
                    {
                        object obj = Activator.CreateInstance(itemType); ;
                        ITableRow ti = obj as ITableRow;
                        for (int j = 0; j < tableArr[i].Length; j++)
                            ti.SetValue(j, tableArr[i][j]?.Trim());
                        items.Add(ti);
                    }
                }
                if (items.Count > 0)
                {
                    InsertItems(null, items, level, FocusItem);
                    return;
                }
            }
        }

        internal void InsertItems(object parent, List<object> items, int itemsLevel, ViewItem target)
        {
            if (items == null || items.Count == 0 || (parent == this && target != null && target.Selected)) return;
            if (ItemsCollection.Sort.SortType != SortProperties.SortTypes.None)
            {
                MessageBox.Show("Корректная вставка объектов возможна только после отмены сортировки");
                return;
            }
            if (ItemsCollection.Filter.Any)
            {
                MessageBox.Show("Корректная вставка объектов возможна только после отмены фильтрации");
                return;
            }
            List<UndoRedoManager.RemoveCommand> removeCommands = null;
            UndoRedoManager.PasteCommand pasteCommand = null;
            IsInserting = true;
            bool select = false;
            if (parent == this)
                removeCommands = Remove(false, true);
            if (ItemsCollection.Count == 0)
            {
                if (PasteOptions.PasteType == PasteOptions.PasteTypes.InChildren || (PasteOptions.PasteType == PasteOptions.PasteTypes.InLevel && (itemsLevel == 1 || itemsLevel == int.MaxValue)))
                {
                    ItemsCollection.Insert(0, items);
                    if (IsUndoRedo)
                        pasteCommand = new UndoRedoManager.PasteCommand(items, null, 0);
                    select = true;
                }
            }
            else if ((target == null ? ItemsCollection.Count - 1 : target.Index) is int index && index >= 0)
            {
                if (PasteOptions.PasteType == PasteOptions.PasteTypes.InLevel)
                {
                    object selectItem = ItemsCollection.GetItem(index);
                    IChildCollection selectColl = selectItem as IChildCollection;
                    if (selectColl == null || itemsLevel <= selectColl.DropLevel)
                    {
                        while (selectColl != null && itemsLevel != selectColl.DropLevel)
                            selectColl = selectColl.Parent as IChildCollection;
                        if (selectColl == null || (itemsLevel == 1 && selectColl.DropLevel == 1))
                        {
                            if (ItemsCollection.SourceContainer.IndexOf(selectColl == null ? selectItem : selectColl) is int iIndex && iIndex != -1)
                            {
                                ItemsCollection.Insert(iIndex + 1, items);
                                if (IsUndoRedo)
                                    pasteCommand = new UndoRedoManager.PasteCommand(items, null, iIndex + 1);
                                select = true;
                            }
                        }
                        else if (selectColl.DropLevel == itemsLevel && selectColl.Parent is IChildCollection pcoll)
                            for (int j = 0; j < pcoll.Count; j++)
                                if (pcoll[j] == selectColl)
                                {
                                    pcoll.InsertRange(j + 1, items);
                                    if (IsUndoRedo)
                                        pasteCommand = new UndoRedoManager.PasteCommand(items, pcoll, j + 1);
                                    select = true;
                                    break;
                                }
                    }
                    else if (selectColl != null && itemsLevel == selectColl.DropLevel + 1)
                    {
                        int iIndex = selectColl.Count;
                        selectColl.InsertRange(iIndex, items);
                        selectColl.Dropped = true;
                        if (IsUndoRedo)
                            pasteCommand = new UndoRedoManager.PasteCommand(items, selectColl, iIndex);
                        select = true;
                    }
                }
                else if (PasteOptions.PasteType == PasteOptions.PasteTypes.InChildren && ItemsCollection.GetItem(index) is IChildCollection collection)
                {
                    int iIndex = collection.Count - 1;
                    collection.InsertRange(iIndex, items);
                    collection.Dropped = true;
                    if (IsUndoRedo)
                        pasteCommand = new UndoRedoManager.PasteCommand(items, collection, iIndex);
                    select = true;
                }
            }
            if (select)
                Select(items);
            if (IsUndoRedo && (removeCommands != null || pasteCommand != null))
                UndoRedoManager.RegistredNewCommand(new UndoRedoManager.MoveCommand(removeCommands, pasteCommand));
            IsInserting = false;
        }

        public virtual List<object> FilesToItem(string[] files, ref int level)
        {
            return FilesToItem(files);
        }

        public virtual List<object> FilesToItem(string[] files)
        {
            return FilesToItemConverter == null ? new List<object>() : FilesToItemConverter.Invoke();
        }

        public void SpecialInsertItem(List<object> items, int level, ViewItem target)
        {
            InsertItems(null, items, level, target);
        }
    }
}