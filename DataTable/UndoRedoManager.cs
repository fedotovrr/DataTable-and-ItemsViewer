using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItemsViewer;

namespace DataTable
{
    /// <summary>
    /// Класс управления отменой и возвратом действий
    /// </summary>
    internal class UndoRedoManager
    {
        private List<object> _buffer = new List<object>();
        private int _LastIndex = -1;
        private Table _parent;

        /// <summary>
        /// Выполняется ли в данный момент действие отмены или возврата
        /// </summary>
        internal bool IsPerforming { get; private set; }

        private readonly object Lock = new object();

        internal UndoRedoManager(Table parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Отмена
        /// </summary>
        public void Undo()
        {
            lock (Lock)
            {
                IsPerforming = true;
                if (_LastIndex < _buffer.Count && _LastIndex >= 0)
                {
                    if (_buffer[_LastIndex] is List<ReadCommand> reads)
                    {
                        int column = -1;
                        for (int i = 0; i < reads.Count; i++)
                            if (reads[i].Item is ITableRow rowItem)
                            {
                                column = reads[i].Column;
                                rowItem.SetValue(reads[i].Column, reads[i].OldValue);
                                UndropParents(reads[i].Item);
                            }
                        _LastIndex--;
                        _parent.IsActual = false;
                        _parent.Select(reads.Select(x => x.Item));
                        if (column > -1) _parent.FocusColumn = column;
                    }
                    else if (_buffer[_LastIndex] is List<RemoveCommand> removes)
                    {
                        for (int i = removes.Count - 1; i >= 0; i--)
                            if (removes[i].Parent == null)
                                _parent.ItemsCollection.Insert(removes[i].Index, new List<object> { removes[i].Item });
                            else if (removes[i].Parent is IChildCollection collection)
                            {
                                collection.Insert(removes[i].Index, removes[i].Item);
                                collection.Dropped = true;
                                UndropParents(collection);
                            }
                        _LastIndex--;
                        _parent.IsActual = false;
                        _parent.Select(removes.Select(x => x.Item));
                    }
                    else if (_buffer[_LastIndex] is MoveCommand move)
                    {
                        if (move.PasteCommand != null)
                        {
                            if (move.PasteCommand.Parent == null)
                                _parent.ItemsCollection.Remove(move.PasteCommand.Items, false);
                            else if (move.PasteCommand.Parent is IChildCollection collection)
                                for (int i = 0; i < move.PasteCommand.Items.Count; i++)
                                    collection.Remove(move.PasteCommand.Items[i]);
                        }
                        if (move.RemoveItems != null && move.RemoveItems.Count > 0)
                        {
                            for (int i = move.RemoveItems.Count - 1; i >= 0; i--)
                            {
                                if (move.RemoveItems[i].Parent == null)
                                    _parent.ItemsCollection.Insert(move.RemoveItems[i].Index, new List<object> { move.RemoveItems[i].Item });
                                else if (move.RemoveItems[i].Parent is IChildCollection collection)
                                {
                                    collection.Insert(move.RemoveItems[i].Index, move.RemoveItems[i].Item);
                                    collection.Dropped = true;
                                    UndropParents(collection);
                                }
                            }
                            _parent.Select(move.RemoveItems.Select(x => x.Item));
                        }
                        _LastIndex--;
                        _parent.IsActual = false;
                    }
                }
                IsPerforming = false;
            }
        }

        /// <summary>
        /// Повтор, возврат
        /// </summary>
        public void Redo()
        {
            lock (Lock)
            {
                IsPerforming = true;
                int index = _LastIndex + 1;
                if (index < _buffer.Count && index >= 0)
                {
                    if (_buffer[index] is List<ReadCommand> reads)
                    {
                        int column = -1;
                        for (int i = 0; i < reads.Count; i++)
                            if (reads[i].Item is ITableRow rowItem)
                            {
                                column = reads[i].Column;
                                rowItem.SetValue(reads[i].Column, reads[i].NewValue);
                                UndropParents(reads[i].Item);
                            }
                        _LastIndex++;
                        _parent.IsActual = false;
                        _parent.Select(reads.Select(x => x.Item));
                        if (column > -1) _parent.FocusColumn = column;
                    }
                    else if (_buffer[index] is List<RemoveCommand> removes)
                    {
                        _parent.ItemsCollection.Remove(removes.Select(x => x.Item).ToList(), false);
                        _LastIndex++;
                        _parent.IsActual = false;
                    }
                    else if (_buffer[index] is MoveCommand move)
                    {
                        if (move.RemoveItems != null && move.RemoveItems.Count > 0)
                            _parent.ItemsCollection.Remove(move.RemoveItems.Select(x => x.Item).ToList(), false);
                        if (move.PasteCommand != null)
                        {
                            if (move.PasteCommand.Parent == null)
                                _parent.ItemsCollection.Insert(move.PasteCommand.Index, move.PasteCommand.Items);
                            else if (move.PasteCommand.Parent is IChildCollection collection)
                            {
                                collection.InsertRange(move.PasteCommand.Index, move.PasteCommand.Items);
                                collection.Dropped = true;
                                UndropParents(collection);
                            }
                            _parent.Select(move.PasteCommand.Items);
                        }
                        _LastIndex++;
                        _parent.IsActual = false;
                    }
                }
                IsPerforming = false;
            }
        }

        /// <summary>
        /// Регистрация новой команды
        /// </summary>
        /// <param name="command"></param>
        public void RegistredNewCommand(object command)
        {
            if (command is List<ReadCommand> || command is List<RemoveCommand> || command is MoveCommand)
            {
                int index = _LastIndex + 1;
                if (index < _buffer.Count && index >= 0)
                    _buffer.RemoveRange(index, _buffer.Count - index);
                _buffer.Add(command);
                _LastIndex = _buffer.Count - 1;
            }
        }

        /// <summary>
        /// Очистка буфера команд
        /// </summary>
        public void ClearBuffer()
        {
            _buffer.Clear();
            _LastIndex = -1;
        }

        private void UndropParents(object item)
        {
            while (item is IChildCollection collection && collection.Parent is IChildCollection parent)
            {
                parent.Dropped = true;
                item = parent;
            }
        }

        internal class ReadCommand
        {
            public object Item;
            public string OldValue;
            public string NewValue;
            public int Column;

            public ReadCommand(object item, string oldValue, string newValue, int column)
            {
                this.Item = item;
                this.OldValue = oldValue;
                this.NewValue = newValue;
                this.Column = column;
            }
        }

        internal class RemoveCommand
        {
            public object Item;
            public object Parent;
            public int Index;

            public RemoveCommand(object item, object parent, int index)
            {
                this.Item = item;
                this.Parent = parent;
                this.Index = index;
            }
        }

        internal class PasteCommand
        {
            public List<object> Items;
            public object Parent;
            public int Index;

            public PasteCommand(List<object> items, object parent, int index)
            {
                this.Items = items;
                this.Parent = parent;
                this.Index = index;
            }
        }

        internal class MoveCommand
        {
            public List<RemoveCommand> RemoveItems;
            public PasteCommand PasteCommand;

            public MoveCommand(List<RemoveCommand> removeItems, PasteCommand pasteCommand)
            {
                this.RemoveItems = removeItems;
                this.PasteCommand = pasteCommand;
            }
        }
    }
}
