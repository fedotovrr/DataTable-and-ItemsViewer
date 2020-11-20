using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataTable;

namespace ItemsViewer.Collection
{
    internal class CollectionContainer
    {
        private MethodInfo Counter;
        private MethodInfo Indexer;
        private MethodInfo Remover;
        private MethodInfo Inserter;
        private IList SourceList;
        public object Source;

        public object this[int index] => SourceList != null ? SourceList[index] : Indexer.Invoke(Source, new object[] { index });

        public int Count => SourceList != null ? SourceList.Count : (int)Counter.Invoke(Source, null);

        public CollectionContainer(object collection)
        {
            if (collection == null)
                throw new NullReferenceException();
            if (collection is IList list)
                SourceList = list;
            else
            {
                Type t = collection.GetType();
                Indexer = t.GetMethod("get_Item");
                if (Indexer == null)
                    Indexer = t.GetMethod("Get");
                Counter = t.GetMethod("get_Count");
                if (Counter == null)
                    Counter = t.GetMethod("get_Length");
                if (Counter == null || Indexer == null)
                    throw new Exception("Объект не является коллекцией с поддержкой индексатора");
                Remover = t.GetMethod("Remove");
                Inserter = t.GetMethod("Insert");
            }
            Source = collection;
        }

        public List<UndoRedoManager.RemoveCommand> Remove(List<object> items, bool isUndoRedo)
        {
            List<UndoRedoManager.RemoveCommand> commands = new List<UndoRedoManager.RemoveCommand>(isUndoRedo ? items.Count : 0);
            if (SourceList != null)
                for (int j = 0; j < items.Count; j++)
                {
                    object item = items[j];
                    int count = Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (SourceList[i] == item)
                        {
                            SourceList.RemoveAt(i);
                            if (isUndoRedo)
                                commands.Add(new UndoRedoManager.RemoveCommand(item, null, i));
                            break;
                        }
                        else if (RemoveChild(SourceList[i] as IChildCollection, item, isUndoRedo ? commands : null))
                            break;
                    }
                }
            else if (Remover != null && Indexer != null)
                for (int j = 0; j < items.Count; j++)
                {
                    object item = items[j];
                    int count = Count;
                    for (int i = 0; i < count; i++)
                    {
                        object iitem = Indexer.Invoke(Source, new object[] { i });
                        if (iitem == item)
                        {
                            Remover.Invoke(Source, new object[] { item });
                            if (isUndoRedo)
                                commands.Add(new UndoRedoManager.RemoveCommand(item, null, i));
                            break;
                        }
                        else if (RemoveChild(iitem as IChildCollection, item, commands))
                            break;
                    }
                }
            return isUndoRedo ? commands : null;
        }

        private bool RemoveChild(IChildCollection collection, object item, List<UndoRedoManager.RemoveCommand> commands)
        {
            if (collection == null)
                return false;
            for (int i = 0; i < collection.Count; i++)
            {
                if (collection[i] == item)
                {
                    collection.Remove(item);
                    commands?.Add(new UndoRedoManager.RemoveCommand(item, collection, i));
                    return true;
                }
                else
                    if (RemoveChild(collection[i] as IChildCollection, item, commands))
                    return true;
            }
            return false;
        }

        public void Insert(int index, List<object> items)
        {
            if (index >= 0 && index <= Count)
            {
                if (SourceList != null)
                    for (int i = items.Count - 1; i >= 0; i--)
                        SourceList.Insert(index, items[i]);
                else if (Inserter != null && Indexer != null)
                    for (int i = 0; i < items.Count; i++)
                        Inserter.Invoke(Source, new object[] { index, items[i] });
            }
        }

        public int IndexOf(object item)
        {
            if (SourceList != null)
                return SourceList.IndexOf(item);
            else if (Remover != null && Indexer != null)
                {
                    int count = Count;
                    for (int i = 0; i < count; i++)
                        if (Indexer.Invoke(Source, new object[] { i }) == item)
                            return i;
                }
            return -1;
        }

        public void Dispose()
        {
            Counter = null;
            Indexer = null;
            SourceList = null;
            Source = null;
        }
    }
}
